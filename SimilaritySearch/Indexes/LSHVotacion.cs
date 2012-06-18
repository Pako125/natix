//
//   Copyright 2012 Francisco Alberto Santoyo Valdez <psantoyo@dep.fie.umich.mx>
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
//   Original filename: natix/SimilaritySearch/Indexes/LSC.cs
// 

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Linq;
using System.Threading;
using natix.CompactDS;

namespace natix.SimilaritySearch
{
	public class LSHVotacion
	{
		static double perror = 0.25;
		static int candidatos = 4;
		
		public static void Prueba(string listname, string qlist){
			
			int S=3;
			int Q=90;
			try{
				testLSHV(listname, qlist, Q, S);
			}catch(Exception e){
				Console.WriteLine("ErrorPrueba ");
				Console.WriteLine(" {0}",e);
				throw e;
			}
		}
		
		public static void testLSHV(string listname, string qlist, int Q, int S){
			int porcen=0;
			int numconsult=0;
			Console.WriteLine("<-- Building LSH Multiple Tables -->");
			string indexName = string.Format("Index.LSC.Prueba.xml");
			HammingMLSC I;
			if (!File.Exists(indexName)) {
				I = new HammingMLSC();
				I.SeqBuilder = SequenceBuilders.GetSeqXLB_SArray64 (16);
				if (! (File.Exists(listname + ".header"))) {
					var A = new AudioSpace();
					A.Build(listname, 30*3, 3);
				}
				I.Build(indexName,"audio-space",listname,20,4);
			}
			I = IndexLoader.Load(indexName) as HammingMLSC;
			
			Console.WriteLine("<-- Searching first object -->");
			var aspace = (AudioSpace)I.MainSpace;
			
			foreach (var qname in (new QueryStream(qlist)).Iterate()) {
				numconsult++;
				Console.WriteLine ("<<<--- Inizializing query --->>>");
				var R = SearchAudio (qname, aspace,I);
				if(R.Count>0){
					porcen++;
				}
				Console.WriteLine ("qname: {0}",qname.QRaw);
				foreach ( var p in R){
					Console.WriteLine ("docid: {0}, dist: {1}, name: {2}",p.docid, p.dist, aspace.GetNameFromDocId(p.docid));
				}
				Console.WriteLine ("<<<--- Finalizing query --->>>");
			}
			
			Console.WriteLine("Porcentaje de respuesta: {0}%",porcen*100/numconsult);
		}
		
		public static IResult SearchAudio(CommandQuery qname,AudioSpace aspace,HammingMLSC I){
			ResultTies Respuesta = new ResultTies(candidatos,false); 
			var Rfull = new ResultTies(100,false);
			List<IResult> res_list = new List<IResult>();
			var qext = BinaryHammingSpace.ParseAndLoadFromFile(qname.QRaw,false);
				
			foreach(var idx in I.Indexes){
				Console.WriteLine("XXXXXX: {0}",qname.QRaw);
				
				int numqgrams = (qext.Count - aspace.Q) / aspace.SymbolSize;
				
				var acc = new Dictionary<int, double>();
				
				Rfull = Search1(qext,acc,numqgrams,idx);
				res_list.Add(Rfull);
				
				int count=0;
				double panterior = -1;
				foreach(var p in Rfull){
					
					if(count > 0){
						if(p.dist - panterior > 10){
							break;
						}
					}
					count++;
					panterior = p.dist;
					
				}
				
				if(count < 10){
					foreach(var p in Rfull){
						Respuesta.Push(p.docid,p.dist);
						count--;
						if(count==0)
							return Respuesta;
					}
				}	   
			}
				
			if(Respuesta.Count==0){
				Dictionary<int,double> rcc = new Dictionary<int,double>();
				foreach(var rl in res_list){
					double dist;
					foreach(var respair in rl){
						if(!rcc.TryGetValue(respair.docid,out dist)){
							rcc[respair.docid] = -1;
						}
						else{
							rcc[respair.docid]--;
						}
					}
				}
				Rfull = new ResultTies(100,false);
				foreach(var pair in rcc){
					Rfull.Push(pair.Key,pair.Value);
				}
										
				var RFinal = new Result(100, false);
				
				foreach (var pair in Rfull){
					var audio = aspace.GetAudio (pair.docid);
					var dist = BinaryHammingSpace.DistMinHamming (audio, qext,aspace.SymbolSize);
					RFinal.Push (pair.docid, dist);
				}
					
				var error = (qext.Count/aspace.SymbolSize * 24) * perror;
				//Console.WriteLine ("Error: {0}",error);
				
				foreach(var p in RFinal){
					if(p.dist < error){
						Respuesta.Push(p.docid,p.dist);
					}
					else{
						break;
					}
						
				}
					
			}
			return Respuesta;
		}
		
		public static ResultTies Search1(IList<byte> qext, Dictionary<int,double> acc, int numqgrams, LSC<IList<byte>> I){
			Chronos time = new Chronos();
			var aspace = (AudioSpace)I.MainSpace;
			int numsampleq = numqgrams;
			int skip = numqgrams/numsampleq;
			
			time.Begin();
			for(int sindex=0; sindex <numsampleq; sindex++){
				int qindex = sindex * skip;
				BinQGram qgram = new BinQGram(qext, qindex * aspace.SymbolSize, aspace.Q);
				IResult R = new Result(int.MaxValue, false);
				I.KNNSearch(qgram,-1,R);
					
				HashSet<int> docId = new HashSet<int>();
					
				foreach (var u in R){
					docId.Add(aspace.GetDocIdFromBlockId(u.docid));
				}
					
				foreach (var d in docId){
						
					double dist;
					if(!acc.TryGetValue(d ,out dist)){
						acc[d] = -1;
					}
					else{
						acc[d]--;
					}
				}
			}
			time.End();
			//time.PrintStats("Tiempo de busqueda");
				
			var Rf = new ResultTies(100 , false);
			foreach (var u in acc){
				Rf.Push(u.Key, u.Value);	
			}
			
			return Rf;
		}
	}
}
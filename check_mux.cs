using System; using System.Reflection; using Mux.Csharp.Sdk.Model; foreach(var p in typeof(AssetMetadata).GetProperties()) Console.WriteLine(p.Name);

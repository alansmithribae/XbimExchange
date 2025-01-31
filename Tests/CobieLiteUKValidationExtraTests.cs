﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.CobieLiteUK.Validation.Extensions;
using Xbim.COBieLiteUK;

namespace Tests
{
    [TestClass]
    public class CobieLiteUKValidationExtraTests
    {
        // [TestMethod]
        //public void fixReqs()
        //{
        //    var d = new DirectoryInfo(@"E:\Dev\xbimTeam\XbimExchange\Tests\ValidationFiles");
        //    foreach (var file in d.GetFiles("Lakeside_Restaurant-stage*.json"))
        //    {
        //        var cat = new Category()
        //        {
        //            Classification = "DPoW",
        //            Code = "required"
        //        };
        //        var fac = Facility.ReadJson(file.FullName);
        //        if (fac.Categories == null)
        //            fac.Categories = new List<Category>(1);
        //        fac.Categories.Add(cat);
        //        foreach (var doc in fac.Get<Document>())
        //        {
        //            if (doc.Categories == null)
        //                doc.Categories = new List<Category>(1);
        //            doc.Categories.Add(cat);
        //        }
        //        fac.WriteJson(file.FullName, false);   
        //    }
        //}

        [TestMethod]
        public void EnumerableStringToSringSuccedes()
        {
            const string value0 = @"Aa";
            const string value1 = @"Bb";

            var testStrings = new []
            {
                @"\",
                @",",
                @"\,",
                @"",
                @"Ciao, bella.",
                @",," ,
                @"\\",
                @"\,",
                @",\"
            }
            ;

            foreach (var str in testStrings)
            {
                var t = new List<String> {value0, str, value1};
                Test(t);
            }

            var l0 = new List<String>() { value0, value1 };
            Test(l0);

            
            Test(testStrings);
        }

        private static void Test(IEnumerable<string> startLista)
        {
            var startList = startLista.ToArray();
            var temporaryString = startList.ListToCompoundString();
            var rebuiltList = temporaryString.CompoundStringToList().ToArray();
            Assert.AreEqual(startList.Count(), rebuiltList.Count());

            var eStart = startList.GetEnumerator();
            var eRebuilt = rebuiltList.GetEnumerator();

            while (eStart.MoveNext() && eRebuilt.MoveNext())
            {
                Assert.AreEqual(eStart.Current, eRebuilt.Current);
            }
        }

        [TestMethod]
        [DeploymentItem("ValidationFiles\\Lakeside_Restaurant-stage6-COBie.json")]
        public void DumpClassificationLists()
        {
            DumpToFile(new FileInfo(@"Lakeside_Restaurant-stage6-COBie.json"));
            DumpToFile(new FileInfo(@"..\..\Lakeside_Restaurant.json"));
        }

        private static void DumpToFile(FileInfo name)
        {
            if (!name.Exists)
                return;
            var destFileName = Path.ChangeExtension(name.Name, @".txt");
            var destFolder = new DirectoryInfo(@"..\..\");
            var destName = Path.Combine(destFolder.FullName, destFileName);

            var req = Facility.ReadJson(name.FullName);
            var cfc = req.GetClassifications();
            var f = new FileInfo(destName);
            using (var textW = f.CreateText())
            {
                foreach (var classific in cfc)
                {
                    textW.WriteLine(classific);
                }
                textW.Close();
            }
        }

        //[TestMethod]
        //[DeploymentItem("ValidationFiles\\Lakeside_Restaurant-stage6-COBie.json")]
        //public void MegaReport()
        //{
        //    var requirement = Facility.ReadJson("Lakeside_Restaurant-stage6-COBie.json");
        //    var submitted = Facility.ReadJson(@"..\..\Lakeside_Restaurant.json");
        //    var retFacility = new Facility {AssetTypes = new List<AssetType>()};

        //    foreach (var assetTypeRequirement in requirement.AssetTypes)
        //    {
        //        var v = new AssetTypeValidator(assetTypeRequirement) { TerminationMode = TerminationMode.ExecuteCompletely };
        //        if (!v.HasRequirements)
        //            continue;
        //        var candidates = submitted.AssetTypes;
        //        foreach (var candidate in candidates)
        //        {
        //            if (retFacility.AssetTypes == null)
        //                retFacility.AssetTypes = new List<AssetType>();
        //            retFacility.AssetTypes.Add(v.Validate(candidate));
        //        }
        //    }
        //    retFacility.WriteJson(@"..\..\megareport.json");
        //}
    }
}

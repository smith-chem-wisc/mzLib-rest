﻿using Chemistry;
using IO.MzML;
using MassSpectrometry;
using MzLibUtil;
using NUnit.Framework;
using Proteomics.AminoAcidPolymer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Test
{
    [TestFixture]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public sealed class TestMzML
    {
        private static Stopwatch Stopwatch { get; set; }

        [SetUp]
        public static void Setuppp()
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
        }

        [TearDown]
        public static void TearDown()
        {
            Console.WriteLine($"Analysis time: {Stopwatch.Elapsed.Hours}h {Stopwatch.Elapsed.Minutes}m {Stopwatch.Elapsed.Seconds}s");
        }

        [OneTimeSetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            UsefulProteomicsDatabases.Loaders.LoadElements();
        }

        [Test]
        public static void AnotherMzMLtest()
        {
            MsDataScan[] scans = new MsDataScan[4];

            double[] intensities1 = new double[] { 1 };
            double[] mz1 = new double[] { 50 };
            MzSpectrum massSpec1 = new MzSpectrum(mz1, intensities1, false);
            scans[0] = new MsDataScan(massSpec1, 1, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, null, "1");

            //ms2
            double[] intensities2 = new double[] { 1 };
            double[] mz2 = new double[] { 30 };
            MzSpectrum massSpec2 = new MzSpectrum(mz2, intensities2, false);
            scans[1] = new MsDataScan(massSpec2, 2, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec2.SumOfAllY, null, null, "2", 50, null, null, 50, 1, DissociationType.CID, 1, null);

            double[] intensities3 = new double[] { 1 };
            double[] mz3 = new double[] { 50 };
            MzSpectrum massSpec3 = new MzSpectrum(mz3, intensities3, false);
            scans[2] = new MsDataScan(massSpec3, 3, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, null, "3");

            //ms2
            double[] intensities4 = new double[] { 1 };
            double[] mz4 = new double[] { 30 };
            MzSpectrum massSpec4 = new MzSpectrum(mz4, intensities4, false);
            scans[3] = new MsDataScan(massSpec4, 4, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec2.SumOfAllY, null, null, "4", 50, null, null, 50, 1, DissociationType.CID, 3, null);

            FakeMsDataFile f = new FakeMsDataFile(scans);

            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(f, Path.Combine(TestContext.CurrentContext.TestDirectory, "what.mzML"), false);

            Mzml ok = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "what.mzML"));

            var scanWithPrecursor = ok.GetAllScansList().Last(b => b.MsnOrder != 1);

            Assert.AreEqual(3, scanWithPrecursor.OneBasedPrecursorScanNumber);
        }

        [Test]
        public static void ReadMzMlInNewEra()
        {
            Dictionary<string, MsDataFile> MyMsDataFiles = new Dictionary<string, MsDataFile>();
            string origDataFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "BinGenerationTest.mzML");
            FilteringParams filter = new FilteringParams(200, 0.01, 1, null, false, false, true);

            MyMsDataFiles[origDataFile] = Mzml.LoadAllStaticData(origDataFile, filter, 1);

            var scans = MyMsDataFiles[origDataFile].GetAllScansList();

            Assert.AreEqual(6, scans[0].MassSpectrum.XArray.Count());
            Assert.AreEqual(20, scans[1].MassSpectrum.XArray.Count());
        }

        [Test]
        public void LoadBadMzml()
        {
            File.Delete(Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "asdfasdfasdfasdfasdf.mzML")); // just to be sure
            Assert.Throws<FileNotFoundException>(() => Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "asdfasdfasdfasdfasdf.mzML")));
        }

        [Test]
        public static void TestPeakTrimmingWithOneWindow()
        {
            int numPeaks = 200;
            double minRatio = 0.01;

            var testFilteringParams = new FilteringParams(numPeaks, minRatio, null, 1, false, true, false);
            List<(double mz, double intensity)> myPeaks = new List<(double mz, double intensity)>();

            for (int mz = 400; mz < 1600; mz++)
            {
                myPeaks.Add((mz, 10d * (double)mz));
            }

            double myMaxIntensity = myPeaks.Max(p => p.intensity);
            var myPeaksOrderedByIntensity = myPeaks.OrderByDescending(p => p.intensity).ToList();
            myPeaksOrderedByIntensity = myPeaksOrderedByIntensity.Take(numPeaks).ToList();
            myPeaksOrderedByIntensity = myPeaksOrderedByIntensity.Where(p => (p.intensity / myMaxIntensity) > minRatio).ToList();
            double sumOfAllIntensities = myPeaksOrderedByIntensity.Sum(p => p.intensity);

            double[] intensities1 = myPeaks.Select(p => p.intensity).ToArray();
            double[] mz1 = myPeaks.Select(p => p.mz).ToArray();

            MzSpectrum massSpec1 = new MzSpectrum(mz1, intensities1, false);
            MsDataScan[] scans = new MsDataScan[]{
                new MsDataScan(massSpec1, 1, 1, true, Polarity.Positive, 1, new MzRange(400, 1600), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, null, "1")
            };
            FakeMsDataFile f = new FakeMsDataFile(scans);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(f, Path.Combine(TestContext.CurrentContext.TestDirectory, "mzml.mzML"), false);

            Mzml ok = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "mzml.mzML"), testFilteringParams);

            int expNumPeaks = ok.GetAllScansList().First().MassSpectrum.XArray.Length;
            double expMinRatio = ok.GetAllScansList().First().MassSpectrum.YArray.Min(p => p / ok.GetAllScansList().First().MassSpectrum.YofPeakWithHighestY).Value;
            List<(double mz, double intensity)> myExpPeaks = new List<(double mz, double intensity)>();

            for (int i = 0; i < ok.GetAllScansList().First().MassSpectrum.YArray.Length; i++)
            {
                myExpPeaks.Add((ok.GetAllScansList().First().MassSpectrum.XArray[i], ok.GetAllScansList().First().MassSpectrum.YArray[i]));
            }

            Assert.That(Math.Round(myMaxIntensity, 0) == Math.Round(ok.GetAllScansList().First().MassSpectrum.YofPeakWithHighestY.Value, 0));
            Assert.That(Math.Round(sumOfAllIntensities, 0) == Math.Round(ok.GetAllScansList().First().MassSpectrum.SumOfAllY, 0));
            Assert.That(myPeaksOrderedByIntensity.Count == ok.GetAllScansList().First().MassSpectrum.XArray.Length);
            Assert.That(expMinRatio >= minRatio);
            Assert.That(!myExpPeaks.Except(myPeaksOrderedByIntensity).Any());
            Assert.That(!myPeaksOrderedByIntensity.Except(myExpPeaks).Any());
        }

        [Test]
        [TestCase(null, null, null, null, null, true, false, 1000)] // no filtering return all peaks
        [TestCase(200, null, null, null, null, true, false, 200)] // top 200 peaks only
        [TestCase(null, 0.01, null, null, null, true, false, 990)] // peaks above intensity ratio
        [TestCase(200, null, null, 2, null, true, false, 400)] // top 200 peaks in each of two windows
        [TestCase(200, null, null, 20, null, true, false, 1000)] // top 200 peaks in each of twenty windows exceeds actual peak count so return all peaks
        [TestCase(200, null, 500, null, null, true, false, 400)] // top 200 peaks in each 500 Da Window
        [TestCase(200, null, 100, null, null, true, false, 1000)] // top 200 peaks in each 100 Da Window exceeds actual peak count so return all peaks
        [TestCase(null, null, null, null, true, true, false, 1000)] // no filtering return all peaks max intensity of 100
        [TestCase(200, null, 500, 20, null, true, false, 400)] // nominal width takes precedent over number of windows
        public static void TestPeakTrimmingVarietyPack(int? numberOfPeaksToKeepPerWindow, double? minimumAllowedIntensityRatioToBasePeak, int? nominalWindowWidthDaltons, int? numberOfWindows, bool normalize, bool applyTrimminToMs1, bool applyTrimmingToMsMs, int expectedPeakCount)
        {
            var testFilteringParams = new FilteringParams(numberOfPeaksToKeepPerWindow, minimumAllowedIntensityRatioToBasePeak, nominalWindowWidthDaltons, numberOfWindows, normalize, applyTrimminToMs1, applyTrimmingToMsMs);
            List<(double mz, double intensity)> myPeaks = new List<(double mz, double intensity)>();

            for (int mz = 1; mz <= 1000; mz++)
            {
                myPeaks.Add((mz, mz));
            }

            double myMaxIntensity = myPeaks.Max(p => p.intensity);

            double[] intensities1 = myPeaks.Select(p => p.intensity).ToArray();
            double[] mz1 = myPeaks.Select(p => p.mz).ToArray();

            MzSpectrum massSpec1 = new MzSpectrum(mz1, intensities1, false);
            MsDataScan[] scans = new MsDataScan[]{
                new MsDataScan(massSpec1, 1, 1, true, Polarity.Positive, 1, new MzRange(1, 1000), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, null, "1")
            };
            FakeMsDataFile f = new FakeMsDataFile(scans);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(f, Path.Combine(TestContext.CurrentContext.TestDirectory, "variety_mzml.mzML"), false);

            Mzml ok = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "variety_mzml.mzML"), testFilteringParams);

            int expNumPeaks = ok.GetAllScansList().First().MassSpectrum.XArray.Length;
            double expMinRatio = ok.GetAllScansList().First().MassSpectrum.YArray.Min(p => p / ok.GetAllScansList().First().MassSpectrum.YofPeakWithHighestY).Value;
            List<(double mz, double intensity)> myExpPeaks = new List<(double mz, double intensity)>();

            for (int i = 0; i < ok.GetAllScansList().First().MassSpectrum.YArray.Length; i++)
            {
                myExpPeaks.Add((ok.GetAllScansList().First().MassSpectrum.XArray[i], ok.GetAllScansList().First().MassSpectrum.YArray[i]));
            }

            Assert.AreEqual(expectedPeakCount, myExpPeaks.Count());

            if (normalize)
            {
                Assert.AreEqual(50.0d, Math.Round(ok.GetAllScansList().First().MassSpectrum.YofPeakWithHighestY.Value, 0));
            }
            else
            {
                Assert.That(Math.Round(myMaxIntensity, 0) == Math.Round(ok.GetAllScansList().First().MassSpectrum.YofPeakWithHighestY.Value, 0));
            }

            if (minimumAllowedIntensityRatioToBasePeak != null && minimumAllowedIntensityRatioToBasePeak > 0)
            {
                Assert.That(expMinRatio >= minimumAllowedIntensityRatioToBasePeak.Value);
            }
            else
            {
                Assert.That(expMinRatio >= 0);
            }
        }

        [Test]
        public static void TestPeakTrimmingWithTooManyWindows()
        {
            Random rand = new Random();
            int numPeaks = 200;
            double minRatio = 0.01;
            int numWindows = 10;

            var testFilteringParams = new FilteringParams(numPeaks, minRatio, null, numWindows, false, true, true);
            // only 1 peak but 10 windows
            List<(double mz, double intensity)> myPeaks = new List<(double mz, double intensity)>
            {
                {(400, rand.Next(1000, 1000000)) }
            };

            double[] intensities1 = myPeaks.Select(p => p.intensity).ToArray();
            double[] mz1 = myPeaks.Select(p => p.mz).ToArray();

            MzSpectrum massSpec1 = new MzSpectrum(mz1, intensities1, false);
            MsDataScan[] scans = new MsDataScan[]{
                new MsDataScan(massSpec1, 1, 1, true, Polarity.Positive, 1, new MzRange(400, 1600), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, null, "1")
            };
            FakeMsDataFile f = new FakeMsDataFile(scans);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(f, Path.Combine(TestContext.CurrentContext.TestDirectory, "mzml.mzML"), false);

            Mzml ok = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "mzml.mzML"), testFilteringParams);

            Assert.That(Math.Round(myPeaks[0].intensity, 0) == Math.Round(ok.GetAllScansList().First().MassSpectrum.YofPeakWithHighestY.Value, 0));
            Assert.That(Math.Round(myPeaks[0].intensity, 0) == Math.Round(ok.GetAllScansList().First().MassSpectrum.SumOfAllY, 0));
            Assert.That(1 == ok.GetAllScansList().First().MassSpectrum.XArray.Length);
            Assert.That(Math.Round(myPeaks[0].mz, 0) == Math.Round(ok.GetAllScansList().First().MassSpectrum.XArray[0], 0));
            Assert.That(Math.Round(myPeaks[0].intensity, 0) == Math.Round(ok.GetAllScansList().First().MassSpectrum.YArray[0], 0));
        }

        [Test]
        public static void WriteEmptyScan()
        {
            double[] intensities1 = new double[] { };
            double[] mz1 = new double[] { };
            MzSpectrum massSpec1 = new MzSpectrum(mz1, intensities1, false);
            MsDataScan[] scans = new MsDataScan[]{
                new MsDataScan(massSpec1, 1, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, null, "1")
            };
            FakeMsDataFile f = new FakeMsDataFile(scans);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(f, Path.Combine(TestContext.CurrentContext.TestDirectory, "mzmlWithEmptyScan.mzML"), false);

            Mzml ok = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "mzmlWithEmptyScan.mzML"));

            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(ok, Path.Combine(TestContext.CurrentContext.TestDirectory, "mzmlWithEmptyScan2.mzML"), false);

            var testFilteringParams = new FilteringParams(200, 0.01, null, 5, false, true, true);
            ok = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "mzmlWithEmptyScan2.mzML"), testFilteringParams);
        }

        [Test]
        public static void DifferentAnalyzersTest()
        {
            MsDataScan[] scans = new MsDataScan[2];

            double[] intensities1 = new double[] { 1 };
            double[] mz1 = new double[] { 50 };
            MzSpectrum massSpec1 = new MzSpectrum(mz1, intensities1, false);
            scans[0] = new MsDataScan(massSpec1, 1, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, null, "1");

            double[] intensities2 = new double[] { 1 };
            double[] mz2 = new double[] { 30 };
            MzSpectrum massSpec2 = new MzSpectrum(mz2, intensities2, false);
            scans[1] = new MsDataScan(massSpec2, 2, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.IonTrap3D, massSpec2.SumOfAllY, null, null, "2", 50, null, null, 50, 1, DissociationType.CID, 1, null);

            FakeMsDataFile f = new FakeMsDataFile(scans);

            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(f, Path.Combine(TestContext.CurrentContext.TestDirectory, "asdfefsf.mzML"), false);

            Mzml ok = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "asdfefsf.mzML"));

            Assert.AreEqual(MZAnalyzerType.Orbitrap, ok.GetAllScansList().First().MzAnalyzer);
            Assert.AreEqual(MZAnalyzerType.IonTrap3D, ok.GetAllScansList().Last().MzAnalyzer);
        }

        [Test]
        public void LoadMzmlTest()
        {
            Assert.Throws<AggregateException>(() =>
            {
                Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "tiny.pwiz.1.1.mzML"));
            }, "Reading profile mode mzmls not supported");
        }

        [Test]
        public void EliminateZeroIntensityPeaksFromMzmlOnFileLoad()
        {
            //create an mzML file with zero intensity peaks in both MS1 and MS2 scans
            MzSpectrum spectrum1 = new MzSpectrum(new double[] { 1, 2, 3 }, new double[] { 0, 1, 2 }, false);
            MzSpectrum spectrum2 = new MzSpectrum(new double[] { 2, 3, 4 }, new double[] { 0, 2, 4 }, false);
            MsDataScan[] scans = new MsDataScan[2];
            scans[0] = new MsDataScan(spectrum1, 1, 1, true, Polarity.Positive, 1.0, new MzRange(300, 2000), "scan filter", MZAnalyzerType.Unknown, spectrum1.SumOfAllY, null, null, null);
            scans[1] = new MsDataScan(spectrum2, 2, 2, true, Polarity.Positive, 1, new MzRange(300, 2000), "scan filter", MZAnalyzerType.Unknown, spectrum2.SumOfAllY, 1, new double[,] { }, "nativeId", 2, 1, 1, 1, 1, DissociationType.Unknown, 1, 2, null);

            MsDataFile testFile = new MsDataFile(scans, new SourceFile("no nativeID format", "mzML format", null, null, null));

            //check that scans have zero intensities
            Assert.IsTrue(testFile.GetAllScansList()[0].MassSpectrum.YArray.Contains(0));
            Assert.IsTrue(testFile.GetAllScansList()[1].MassSpectrum.YArray.Contains(0));

            //write the mzML file with zero intensity scans
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(testFile, "mzMLWithZeros.mzML", false);

            //read the mzML file. zero intensity peaks should be eliminated during read
            Mzml readFile = Mzml.LoadAllStaticData("mzMLWithZeros.mzML", null, 1);

            //insure that read scans contain no zero intensity peaks
            Assert.IsFalse(readFile.GetAllScansList()[0].MassSpectrum.YArray.Contains(0));
            Assert.IsFalse(readFile.GetAllScansList()[1].MassSpectrum.YArray.Contains(0));

            File.Delete("mzMLWithZeros.mzML");
        }

        [Test]
        public void LoadMzmlFromConvertedMGFTest()
        {
            Mzml a = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "tester.mzML"));

            var ya = a.GetOneBasedScan(1).MassSpectrum;
            Assert.AreEqual(192, ya.Size);
            var ya2 = a.GetOneBasedScan(3).MassSpectrum;
            Assert.AreEqual(165, ya2.Size);
            var ya3 = a.GetOneBasedScan(5).MassSpectrum;
            Assert.AreEqual(551, ya3.Size);

            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(a, "CreateFileFromConvertedMGF.mzML", false);

            Mzml b = Mzml.LoadAllStaticData(@"CreateFileFromConvertedMGF.mzML");

            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(b, "CreateFileFromConvertedMGF2.mzML", false);
        }

        [Test]
        public void WriteMzmlTest()
        {
            var peptide = new Peptide("GPEAPPPALPAGAPPPCTAVTSDHLNSLLGNILR");
            OldSchoolChemicalFormulaModification carbamidomethylationOfCMod = new OldSchoolChemicalFormulaModification(ChemicalFormula.ParseFormula("H3C2NO"), "carbamidomethylation of C", ModificationSites.C);
            peptide.AddModification(carbamidomethylationOfCMod);

            MzSpectrum MS1 = CreateSpectrum(peptide.GetChemicalFormula(), 300, 2000, 1);

            MzSpectrum MS2 = CreateMS2spectrum(peptide.Fragment(FragmentTypes.b | FragmentTypes.y, true), 100, 1500);

            MsDataScan[] Scans = new MsDataScan[2];

            Scans[0] = new MsDataScan(MS1, 1, 1, true, Polarity.Positive, 1.0, new MzRange(300, 2000), " first spectrum", MZAnalyzerType.Unknown, MS1.SumOfAllY, 1, null, "scan=1");
            //            scans[1] = new MsDataScanZR(massSpec2, 2, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.IonTrap3D, massSpec2.SumOfAllY, null, null, "2", 50, null, null, 50, 1, DissociationType.CID, 1, null);

            Scans[1] = new MsDataScan(MS2, 2, 2, true, Polarity.Positive, 2.0, new MzRange(100, 1500), " second spectrum", MZAnalyzerType.Unknown, MS2.SumOfAllY, 1, null, "scan=2", 1134.26091302033, 3, 0.141146966879759, 1134.3, 1, DissociationType.Unknown, 1, 1134.26091302033);

            var myMsDataFile = new FakeMsDataFile(Scans);

            var oldFirstValue = myMsDataFile.GetOneBasedScan(1).MassSpectrum.FirstX;

            var secondScan = myMsDataFile.GetOneBasedScan(2);
            Assert.AreEqual(1, secondScan.IsolationRange.Maximum - secondScan.IsolationRange.Minimum);

            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(myMsDataFile, "argh.mzML", false);

            Mzml okay = Mzml.LoadAllStaticData(@"argh.mzML");
            okay.GetOneBasedScan(2);

            Assert.AreEqual(1, okay.GetClosestOneBasedSpectrumNumber(1));
            Assert.AreEqual(2, okay.GetClosestOneBasedSpectrumNumber(2));

            var newFirstValue = okay.GetOneBasedScan(1).MassSpectrum.FirstX;
            Assert.AreEqual(oldFirstValue.Value, newFirstValue.Value, 1e-9);

            var secondScan2 = okay.GetOneBasedScan(2);

            Assert.AreEqual(1, secondScan2.IsolationRange.Maximum - secondScan2.IsolationRange.Minimum);

            secondScan2.MassSpectrum.ReplaceXbyApplyingFunction((a) => 44);
            Assert.AreEqual(44, secondScan2.MassSpectrum.LastX);
        }

        [Test]
        public void MzmlFindPrecursorReferenceScan()
        {
            //some ms2 scans dont have properly assigned precursor scans
            //this unit test is intended to test the fallback option in MzML\Mzml.cs @ lines 459-47
            //constructs three ms1 scans and three ms2 scans
            //if ms2 scan precusor reference is null, assumes most recent ms1 scan is correct reference

            MsDataScan[] scans = new MsDataScan[6];
            MsDataScan[] scans1 = new MsDataScan[4];

            double[] intensities0 = new double[] { 1 };
            double[] mz0 = new double[] { 50 };
            MzSpectrum massSpec0 = new MzSpectrum(mz0, intensities0, false);
            scans[0] = new MsDataScan(massSpec0, 1, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec0.SumOfAllY, null, null, "1");
            scans1[0] = scans[0];

            double[] intensities1 = new double[] { 1 };
            double[] mz1 = new double[] { 50 };
            MzSpectrum massSpec1 = new MzSpectrum(mz1, intensities1, false);
            scans[1] = new MsDataScan(massSpec0, 2, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, null, "1");

            double[] intensities2 = new double[] { 1 };
            double[] mz2 = new double[] { 50 };
            MzSpectrum massSpec2 = new MzSpectrum(mz2, intensities2, false);
            scans[2] = new MsDataScan(massSpec2, 3, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec2.SumOfAllY, null, null, "1");

            //ms2
            double[] intensities3 = new double[] { 1 };
            double[] mz3 = new double[] { 30 };
            MzSpectrum massSpec3 = new MzSpectrum(mz3, intensities3, false);
            scans[3] = new MsDataScan(massSpec3, 4, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec3.SumOfAllY, null, null, "2", 50, null, null, 50, 1, DissociationType.CID, 3, null);
            scans1[1] = new MsDataScan(massSpec3, 2, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec3.SumOfAllY, null, null, "2", 50, null, null, 50, 1, DissociationType.CID, 1, null);

            //ms2
            double[] intensities4 = new double[] { 1 };
            double[] mz4 = new double[] { 30 };
            MzSpectrum massSpec4 = new MzSpectrum(mz4, intensities4, false);
            scans[4] = new MsDataScan(massSpec4, 5, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec4.SumOfAllY, null, null, "2", 50, null, null, 50, 1, DissociationType.CID, 3, null);
            scans1[2] = new MsDataScan(massSpec4, 3, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec4.SumOfAllY, null, null, "2", 50, null, null, 50, 1, DissociationType.CID, 1, null);

            //ms2
            double[] intensities5 = new double[] { 1 };
            double[] mz5 = new double[] { 30 };
            MzSpectrum massSpec5 = new MzSpectrum(mz5, intensities5, false);
            scans[5] = new MsDataScan(massSpec5, 6, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec5.SumOfAllY, null, null, "4", 50, null, null, 50, 1, DissociationType.CID, null, null);
            scans1[3] = new MsDataScan(massSpec5, 4, 2, true, Polarity.Positive, 2, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec5.SumOfAllY, null, null, "4", 50, null, null, 50, 1, DissociationType.CID, null, null);

            FakeMsDataFile fakeFile = new FakeMsDataFile(scans);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(fakeFile, Path.Combine(TestContext.CurrentContext.TestDirectory, "what.mzML"), false);
            Mzml fakeMzml = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "what.mzML"));

            FakeMsDataFile fakeFile1 = new FakeMsDataFile(scans1);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(fakeFile1, Path.Combine(TestContext.CurrentContext.TestDirectory, "what1.mzML"), false);
            Mzml fakeMzml1 = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory, "what1.mzML"));

            Assert.AreEqual(3, fakeMzml.GetAllScansList().ElementAt(5).OneBasedPrecursorScanNumber);
            Assert.AreEqual(1, fakeMzml1.GetAllScansList().ElementAt(3).OneBasedPrecursorScanNumber);
        }

        [Test]
        [TestCase("tester.mzml")]
        [TestCase("SmallCalibratibleYeast.mzml")]
        [TestCase("small.raw", true)]
        [TestCase("small.raw", false)]
        [TestCase("testFileWMS2.raw", true)]
        [TestCase("testFileWMS2.raw", false)]
        [TestCase("testFileWMS2.raw", false)]
        [TestCase("05-13-16_cali_MS_60K-res_MS.raw", true)]
        [TestCase("05-13-16_cali_MS_60K-res_MS.raw", false)]
        public static void TestDynamicMzml(string fileName, bool writeIndexed = false)
        {
            if (Path.GetExtension(fileName).ToUpper() == ".RAW")
            {
                string append = writeIndexed ? "_indexed" : "_unindexed";
                string rawPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", fileName);
                var raw = IO.ThermoRawFileReader.ThermoRawFileReader.LoadAllStaticData(rawPath);
                string mzmlFilename = Path.GetFileNameWithoutExtension(fileName) + append + ".mzML";
                fileName = mzmlFilename;
                string mzmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", fileName);

                MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(raw, mzmlPath, writeIndexed);
            }

            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", fileName);

            Mzml staticMzml = Mzml.LoadAllStaticData(filePath);
            MzmlDynamicData dynamicMzml = new MzmlDynamicData(filePath);

            foreach (MsDataScan staticScan in staticMzml.GetAllScansList())
            {
                MsDataScan dynamicScan = dynamicMzml.GetOneBasedScanFromDynamicConnection(staticScan.OneBasedScanNumber);

                Assert.That(dynamicScan.OneBasedScanNumber == staticScan.OneBasedScanNumber);
                Assert.That(dynamicScan.MsnOrder == staticScan.MsnOrder);

                if (!double.IsNaN(dynamicScan.RetentionTime) || !double.IsNaN(staticScan.RetentionTime))
                {
                    Assert.That(dynamicScan.RetentionTime == staticScan.RetentionTime);
                }

                Assert.That(dynamicScan.Polarity == staticScan.Polarity);

                if (!double.IsNaN(staticScan.ScanWindowRange.Minimum) || !double.IsNaN(staticScan.ScanWindowRange.Maximum)
                    || !double.IsNaN(dynamicScan.ScanWindowRange.Minimum) || !double.IsNaN(dynamicScan.ScanWindowRange.Maximum))
                {
                    Assert.That(dynamicScan.ScanWindowRange.Minimum == staticScan.ScanWindowRange.Minimum);
                    Assert.That(dynamicScan.ScanWindowRange.Maximum == staticScan.ScanWindowRange.Maximum);
                }

                Assert.That(dynamicScan.ScanFilter == staticScan.ScanFilter);
                Assert.That(dynamicScan.NativeId == staticScan.NativeId);
                Assert.That(dynamicScan.IsCentroid == staticScan.IsCentroid);
                Assert.That(dynamicScan.TotalIonCurrent == staticScan.TotalIonCurrent);
                Assert.That(dynamicScan.InjectionTime == staticScan.InjectionTime);
                Assert.That(dynamicScan.NoiseData == staticScan.NoiseData);

                Assert.That(dynamicScan.IsolationMz == staticScan.IsolationMz);
                Assert.That(dynamicScan.SelectedIonChargeStateGuess == staticScan.SelectedIonChargeStateGuess);
                Assert.That(dynamicScan.SelectedIonIntensity == staticScan.SelectedIonIntensity);
                Assert.That(dynamicScan.SelectedIonMZ == staticScan.SelectedIonMZ);
                Assert.That(dynamicScan.DissociationType == staticScan.DissociationType);

                if (dynamicScan.IsolationWidth != null || staticScan.IsolationWidth != null)
                {
                    if (!double.IsNaN(dynamicScan.IsolationWidth.Value) || !double.IsNaN(staticScan.IsolationWidth.Value))
                    {
                        Assert.That(dynamicScan.IsolationWidth == staticScan.IsolationWidth);
                    }
                }

                Assert.That(dynamicScan.OneBasedPrecursorScanNumber == staticScan.OneBasedPrecursorScanNumber);
                Assert.That(dynamicScan.SelectedIonMonoisotopicGuessIntensity == staticScan.SelectedIonMonoisotopicGuessIntensity);
                Assert.That(dynamicScan.SelectedIonMonoisotopicGuessMz == staticScan.SelectedIonMonoisotopicGuessMz);

                if (dynamicScan.IsolationRange != null || staticScan.IsolationRange != null)
                {
                    Assert.That(dynamicScan.IsolationRange.Minimum == staticScan.IsolationRange.Minimum);
                    Assert.That(dynamicScan.IsolationRange.Maximum == staticScan.IsolationRange.Maximum);
                }

                Assert.That(dynamicScan.MassSpectrum.XArray.Length == staticScan.MassSpectrum.XArray.Length);
                Assert.That(dynamicScan.MassSpectrum.YArray.Length == staticScan.MassSpectrum.YArray.Length);

                for (int i = 0; i < staticScan.MassSpectrum.XArray.Length; i++)
                {
                    double staticMz = staticScan.MassSpectrum.XArray[i];
                    double staticIntensity = staticScan.MassSpectrum.YArray[i];

                    double dynamicMz = dynamicScan.MassSpectrum.XArray[i];
                    double dynamicIntensity = dynamicScan.MassSpectrum.YArray[i];

                    Assert.That(dynamicMz == staticMz);
                    Assert.That(dynamicIntensity == staticIntensity);
                }
            }
        }

        [Test]
        public static void TestDynamicMzmlWithPeakFiltering()
        {
            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "SmallCalibratibleYeast.mzml");
            FilteringParams filteringParams = new FilteringParams(200, 0.01, 1, null, false, true, true);

            Mzml staticMzml = Mzml.LoadAllStaticData(filePath, filteringParams);
            MzmlDynamicData dynamicMzml = new MzmlDynamicData(filePath);

            foreach (MsDataScan staticScan in staticMzml.GetAllScansList())
            {
                if (staticScan.OneBasedScanNumber > 10)
                {
                    // only the first 10 scans are checked because checking the entire file takes 7min on AppVeyor
                    break;
                }

                MsDataScan dynamicScan = dynamicMzml.GetOneBasedScanFromDynamicConnection(staticScan.OneBasedScanNumber, filteringParams);

                Assert.That(dynamicScan.OneBasedScanNumber == staticScan.OneBasedScanNumber);
                Assert.That(dynamicScan.MsnOrder == staticScan.MsnOrder);

                if (!double.IsNaN(dynamicScan.RetentionTime) || !double.IsNaN(staticScan.RetentionTime))
                {
                    Assert.That(dynamicScan.RetentionTime == staticScan.RetentionTime);
                }

                Assert.That(dynamicScan.Polarity == staticScan.Polarity);

                if (!double.IsNaN(staticScan.ScanWindowRange.Minimum) || !double.IsNaN(staticScan.ScanWindowRange.Maximum)
                    || !double.IsNaN(dynamicScan.ScanWindowRange.Minimum) || !double.IsNaN(dynamicScan.ScanWindowRange.Maximum))
                {
                    Assert.That(dynamicScan.ScanWindowRange.Minimum == staticScan.ScanWindowRange.Minimum);
                    Assert.That(dynamicScan.ScanWindowRange.Maximum == staticScan.ScanWindowRange.Maximum);
                }

                Assert.That(dynamicScan.ScanFilter == staticScan.ScanFilter);
                Assert.That(dynamicScan.NativeId == staticScan.NativeId);
                Assert.That(dynamicScan.IsCentroid == staticScan.IsCentroid);
                Assert.That(dynamicScan.TotalIonCurrent == staticScan.TotalIonCurrent);
                Assert.That(dynamicScan.InjectionTime == staticScan.InjectionTime);
                Assert.That(dynamicScan.NoiseData == staticScan.NoiseData);

                Assert.That(dynamicScan.IsolationMz == staticScan.IsolationMz);
                Assert.That(dynamicScan.SelectedIonChargeStateGuess == staticScan.SelectedIonChargeStateGuess);
                Assert.That(dynamicScan.SelectedIonIntensity == staticScan.SelectedIonIntensity);
                Assert.That(dynamicScan.SelectedIonMZ == staticScan.SelectedIonMZ);
                Assert.That(dynamicScan.DissociationType == staticScan.DissociationType);

                if (dynamicScan.IsolationWidth != null || staticScan.IsolationWidth != null)
                {
                    if (!double.IsNaN(dynamicScan.IsolationWidth.Value) || !double.IsNaN(staticScan.IsolationWidth.Value))
                    {
                        Assert.That(dynamicScan.IsolationWidth == staticScan.IsolationWidth);
                    }
                }

                Assert.That(dynamicScan.OneBasedPrecursorScanNumber == staticScan.OneBasedPrecursorScanNumber);
                Assert.That(dynamicScan.SelectedIonMonoisotopicGuessIntensity == staticScan.SelectedIonMonoisotopicGuessIntensity);
                Assert.That(dynamicScan.SelectedIonMonoisotopicGuessMz == staticScan.SelectedIonMonoisotopicGuessMz);

                if (dynamicScan.IsolationRange != null || staticScan.IsolationRange != null)
                {
                    Assert.That(dynamicScan.IsolationRange.Minimum == staticScan.IsolationRange.Minimum);
                    Assert.That(dynamicScan.IsolationRange.Maximum == staticScan.IsolationRange.Maximum);
                }

                Assert.That(dynamicScan.MassSpectrum.XArray.Length == staticScan.MassSpectrum.XArray.Length);
                Assert.That(dynamicScan.MassSpectrum.YArray.Length == staticScan.MassSpectrum.YArray.Length);

                for (int i = 0; i < staticScan.MassSpectrum.XArray.Length; i++)
                {
                    double staticMz = staticScan.MassSpectrum.XArray[i];
                    double staticIntensity = staticScan.MassSpectrum.YArray[i];

                    double dynamicMz = dynamicScan.MassSpectrum.XArray[i];
                    double dynamicIntensity = dynamicScan.MassSpectrum.YArray[i];

                    Assert.That(dynamicMz == staticMz);
                    Assert.That(dynamicIntensity == staticIntensity);
                }
            }
        }

        [Test]
        public static void TestDynamicMzmlWithMixedBits()
        {
            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "SmallCalibratibleYeast.mzml");
            string filePath2 = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "SmallCalibratibleYeast_mixed_bits.mzml");

            Mzml staticMzml = Mzml.LoadAllStaticData(filePath, null);
            Mzml staticMzml2 = Mzml.LoadAllStaticData(filePath2, null);

            MzmlDynamicData dynamicMzml = new MzmlDynamicData(filePath);
            MzmlDynamicData dynamicMzml2 = new MzmlDynamicData(filePath2);

            foreach (MsDataScan staticScan in staticMzml.GetAllScansList())
            {
                // only the scans 72-74 are checked because checking the entire file takes a long time on AppVeyor
                if (staticScan.OneBasedScanNumber < 72)
                {
                    continue;
                }
                if (staticScan.OneBasedScanNumber > 74)
                {
                    break;
                }

                MsDataScan staticScan2 = staticMzml2.GetOneBasedScan(staticScan.OneBasedScanNumber);
                MsDataScan dynamicScan1 = dynamicMzml.GetOneBasedScanFromDynamicConnection(staticScan.OneBasedScanNumber);
                MsDataScan dynamicScan2 = dynamicMzml2.GetOneBasedScanFromDynamicConnection(staticScan.OneBasedScanNumber);

                Assert.That(staticScan2.OneBasedScanNumber == staticScan.OneBasedScanNumber);
                Assert.That(dynamicScan1.OneBasedScanNumber == staticScan.OneBasedScanNumber);
                Assert.That(dynamicScan2.OneBasedScanNumber == staticScan.OneBasedScanNumber);

                Assert.That(staticScan2.MassSpectrum.XArray.Length == staticScan.MassSpectrum.XArray.Length);
                Assert.That(dynamicScan1.MassSpectrum.XArray.Length == staticScan.MassSpectrum.XArray.Length);
                Assert.That(dynamicScan2.MassSpectrum.XArray.Length == staticScan.MassSpectrum.XArray.Length);

                Assert.That(staticScan2.MassSpectrum.YArray.Length == staticScan.MassSpectrum.YArray.Length);
                Assert.That(dynamicScan1.MassSpectrum.YArray.Length == staticScan.MassSpectrum.YArray.Length);
                Assert.That(dynamicScan2.MassSpectrum.YArray.Length == staticScan.MassSpectrum.YArray.Length);

                for (int i = 0; i < staticScan.MassSpectrum.XArray.Length; i++)
                {
                    double staticMz = staticScan.MassSpectrum.XArray[i];
                    double staticIntensity = staticScan.MassSpectrum.YArray[i];

                    double staticMz2 = staticScan2.MassSpectrum.XArray[i];
                    double staticIntensity2 = staticScan2.MassSpectrum.YArray[i];

                    Assert.That(staticMz2 == staticMz);
                    Assert.That(staticIntensity2 == staticIntensity);

                    double dynamicMz = dynamicScan1.MassSpectrum.XArray[i];
                    double dynamicIntensity = dynamicScan1.MassSpectrum.YArray[i];

                    Assert.That(dynamicMz == staticMz);
                    Assert.That(dynamicIntensity == staticIntensity);

                    double dynamicMz2 = dynamicScan2.MassSpectrum.XArray[i];
                    double dynamicIntensity2 = dynamicScan2.MassSpectrum.YArray[i];

                    Assert.That(dynamicMz2 == staticMz);
                    Assert.That(dynamicIntensity2 == staticIntensity);
                }
            }
        }

        [Test]
        public static void TestEthcdReading()
        {
            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DataFiles", "sliced_ethcd.mzML");
            Mzml mzml = Mzml.LoadAllStaticData(filePath, null, 1);
            var unknownScan = mzml.GetOneBasedScan(3);
            Assert.That(unknownScan.DissociationType == DissociationType.Unknown);
            var hcdScan = mzml.GetOneBasedScan(5);
            Assert.That(hcdScan.DissociationType == DissociationType.HCD);
            var ethcdScan = mzml.GetOneBasedScan(6);
            Assert.That(ethcdScan.DissociationType == DissociationType.EThcD);
        }

        private MzSpectrum CreateMS2spectrum(IEnumerable<Fragment> fragments, int v1, int v2)
        {
            List<double> allMasses = new List<double>();
            List<double> allIntensities = new List<double>();
            foreach (ChemicalFormulaFragment f in fragments)
            {
                var spec = CreateSpectrum(f.ThisChemicalFormula, v1, v2, 2);
                for (int i = 0; i < spec.Size; i++)
                {
                    allMasses.Add(spec.XArray[i]);
                    allIntensities.Add(spec.YArray[i]);
                }
            }
            var allMassesArray = allMasses.ToArray();
            var allIntensitiessArray = allIntensities.ToArray();

            Array.Sort(allMassesArray, allIntensitiessArray);
            return new MzSpectrum(allMassesArray, allIntensitiessArray, false);
        }

        private MzSpectrum CreateSpectrum(ChemicalFormula f, double lowerBound, double upperBound, int minCharge)
        {
            IsotopicDistribution isodist = IsotopicDistribution.GetDistribution(f, 0.1);

            return new MzSpectrum(isodist.Masses.ToArray(), isodist.Intensities.ToArray(), false);
            //massSpectrum1 = massSpectrum1.FilterByNumberOfMostIntense(5);

            //var chargeToLookAt = minCharge;
            //var correctedSpectrum = massSpectrum1.NewSpectrumApplyFunctionToX(s => s.ToMz(chargeToLookAt));

            //List<double> allMasses = new List<double>();
            //List<double> allIntensitiess = new List<double>();

            //while (correctedSpectrum.FirstX > lowerBound)
            //{
            //    foreach (var thisPeak in correctedSpectrum)
            //    {
            //        if (thisPeak.Mz > lowerBound && thisPeak.Mz < upperBound)
            //        {
            //            allMasses.Add(thisPeak.Mz);
            //            allIntensitiess.Add(thisPeak.Intensity);
            //        }
            //    }
            //    chargeToLookAt += 1;
            //    correctedSpectrum = massSpectrum1.NewSpectrumApplyFunctionToX(s => s.ToMz(chargeToLookAt));
            //}

            //var allMassesArray = allMasses.ToArray();
            //var allIntensitiessArray = allIntensitiess.ToArray();

            //Array.Sort(allMassesArray, allIntensitiessArray);

            //return new MzmlMzSpectrum(allMassesArray, allIntensitiessArray, false);
        }
    }
}
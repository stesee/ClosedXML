﻿using ClosedXML.Excel;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace ClosedXML.Tests.Excel.Clearing
{
    [TestFixture]
    public class ClearingTests
    {
        private static readonly XLColor backgroundColor = XLColor.LightBlue;
        private static readonly XLColor foregroundColor = XLColor.DarkBrown;

        private IXLWorkbook SetupWorkbook(XLWorkbook wb)
        {
            var ws = wb.Worksheets.Add("Sheet1");

            var c = ws.FirstCell()
                .SetValue("Hello world!");

            c.GetComment().AddText("Some comment");

            c.Style.Fill.BackgroundColor = backgroundColor;
            c.Style.Font.FontColor = foregroundColor;
            c.CreateDataValidation().Custom("B1");

            ////

            c = ws.FirstCell()
                .CellBelow()
                .SetFormulaA1("=LEFT(A1,5)");

            c.GetComment().AddText("Another comment");

            c.Style.Fill.BackgroundColor = backgroundColor;
            c.Style.Font.FontColor = foregroundColor;

            ////

            c = ws.FirstCell()
                .CellBelow(2)
                .SetValue(new DateTime(2018, 1, 15));

            c.GetComment().AddText("A date");

            c.Style.Fill.BackgroundColor = backgroundColor;
            c.Style.Font.FontColor = foregroundColor;

            ws.Column(1)
                .AddConditionalFormat().WhenStartsWith("Hell")
                .Fill.SetBackgroundColor(XLColor.Red)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thick)
                .Border.SetOutsideBorderColor(XLColor.Blue)
                .Font.SetBold();

            Assert.AreEqual(XLDataType.Text, ws.Cell("A1").DataType);
            Assert.AreEqual(XLDataType.Text, ws.Cell("A2").DataType);
            Assert.AreEqual(XLDataType.DateTime, ws.Cell("A3").DataType);

            Assert.AreEqual(false, ws.Cell("A1").HasFormula);
            Assert.AreEqual(true, ws.Cell("A2").HasFormula);
            Assert.AreEqual(false, ws.Cell("A1").HasFormula);

            foreach (var cell in ws.Range("A1:A3").Cells())
            {
                Assert.AreEqual(backgroundColor, cell.Style.Fill.BackgroundColor);
                Assert.AreEqual(foregroundColor, cell.Style.Font.FontColor);
                Assert.IsTrue(ws.ConditionalFormats.Any());
                Assert.IsTrue(cell.HasComment);
            }

            Assert.AreEqual("B1", ws.Cell("A1").GetDataValidation().Value);

            return wb;
        }

        [Test]
        public void WorksheetClearAll()
        {
            using var emptyWb = new XLWorkbook();

            using var wb = SetupWorkbook(emptyWb);
            var ws = wb.Worksheets.First();

            ws.Clear(XLClearOptions.All);

            foreach (var c in ws.Range("A1:A10").Cells())
            {
                Assert.IsTrue(c.IsEmpty());
                Assert.AreEqual(XLDataType.Text, c.DataType);
                Assert.AreEqual(ws.Style.Fill.BackgroundColor, c.Style.Fill.BackgroundColor);
                Assert.AreEqual(ws.Style.Font.FontColor, c.Style.Font.FontColor);
                Assert.IsFalse(ws.ConditionalFormats.Any());
                Assert.IsFalse(c.HasComment);
                Assert.AreEqual(string.Empty, c.GetDataValidation().Value);
            }
        }

        [Test]
        public void WorksheetClearContents()
        {
            using var emptyWb = new XLWorkbook();

            using var wb = SetupWorkbook(emptyWb);
            var ws = wb.Worksheets.First();

            ws.Clear(XLClearOptions.Contents);

            foreach (var c in ws.Range("A1:A3").Cells())
            {
                Assert.IsTrue(c.IsEmpty(XLCellsUsedOptions.Contents));
                Assert.AreEqual(backgroundColor, c.Style.Fill.BackgroundColor);
                Assert.AreEqual(foregroundColor, c.Style.Font.FontColor);
                Assert.IsTrue(ws.ConditionalFormats.Any());
                Assert.IsTrue(c.HasComment);
            }

            Assert.AreEqual("B1", ws.Cell("A1").GetDataValidation().Value);

            Assert.AreEqual(XLDataType.Text, ws.Cell("A1").DataType);
            Assert.AreEqual(XLDataType.Text, ws.Cell("A2").DataType);
            Assert.AreEqual(XLDataType.DateTime, ws.Cell("A3").DataType);
        }

        [Test]
        public void WorksheetClearDataType()
        {
            using var emptyWb = new XLWorkbook();

            using var wb = SetupWorkbook(emptyWb);
            var ws = wb.Worksheets.First();

            ws.Clear(XLClearOptions.DataType);

            foreach (var c in ws.Range("A1:A3").Cells())
            {
                Assert.IsFalse(c.IsEmpty());
                Assert.AreEqual(XLDataType.Text, c.DataType);
                Assert.AreEqual(backgroundColor, c.Style.Fill.BackgroundColor);
                Assert.AreEqual(foregroundColor, c.Style.Font.FontColor);
                Assert.IsTrue(ws.ConditionalFormats.Any());
                Assert.IsTrue(c.HasComment);
            }

            Assert.AreEqual("B1", ws.Cell("A1").GetDataValidation().Value);
        }

        [Test]
        public void WorksheetClearNormalFormats()
        {
            using var emptyWb = new XLWorkbook();

            using var wb = SetupWorkbook(emptyWb);
            var ws = wb.Worksheets.First();

            ws.Clear(XLClearOptions.NormalFormats);

            foreach (var c in ws.Range("A1:A3").Cells())
            {
                Assert.IsFalse(c.IsEmpty());
                Assert.AreEqual(ws.Style.Fill.BackgroundColor, c.Style.Fill.BackgroundColor);
                Assert.AreEqual(ws.Style.Font.FontColor, c.Style.Font.FontColor);
                Assert.IsTrue(ws.ConditionalFormats.Any());
                Assert.IsTrue(c.HasComment);
            }

            Assert.AreEqual(XLDataType.Text, ws.Cell("A1").DataType);
            Assert.AreEqual(XLDataType.Text, ws.Cell("A2").DataType);
            Assert.AreEqual(XLDataType.DateTime, ws.Cell("A3").DataType);

            Assert.AreEqual("B1", ws.Cell("A1").GetDataValidation().Value);
        }

        [Test]
        public void WorksheetClearConditionalFormats()
        {
            using var emptyWb = new XLWorkbook();

            using var wb = SetupWorkbook(emptyWb);
            var ws = wb.Worksheets.First();

            ws.Clear(XLClearOptions.ConditionalFormats);

            foreach (var c in ws.Range("A1:A3").Cells())
            {
                Assert.IsFalse(c.IsEmpty());
                Assert.AreEqual(backgroundColor, c.Style.Fill.BackgroundColor);
                Assert.AreEqual(foregroundColor, c.Style.Font.FontColor);
                Assert.IsFalse(ws.ConditionalFormats.Any());
                Assert.IsTrue(c.HasComment);
            }

            Assert.AreEqual(XLDataType.Text, ws.Cell("A1").DataType);
            Assert.AreEqual(XLDataType.Text, ws.Cell("A2").DataType);
            Assert.AreEqual(XLDataType.DateTime, ws.Cell("A3").DataType);

            Assert.AreEqual("B1", ws.Cell("A1").GetDataValidation().Value);
        }

        [Test]
        public void WorksheetClearComments()
        {
            using var emptyWb = new XLWorkbook();

            using var wb = SetupWorkbook(emptyWb);
            var ws = wb.Worksheets.First();

            ws.Clear(XLClearOptions.Comments);

            foreach (var c in ws.Range("A1:A3").Cells())
            {
                Assert.IsFalse(c.IsEmpty());
                Assert.AreEqual(backgroundColor, c.Style.Fill.BackgroundColor);
                Assert.AreEqual(foregroundColor, c.Style.Font.FontColor);
                Assert.IsTrue(ws.ConditionalFormats.Any());
                Assert.IsFalse(c.HasComment);
            }

            Assert.AreEqual(XLDataType.Text, ws.Cell("A1").DataType);
            Assert.AreEqual(XLDataType.Text, ws.Cell("A2").DataType);
            Assert.AreEqual(XLDataType.DateTime, ws.Cell("A3").DataType);

            Assert.AreEqual("B1", ws.Cell("A1").GetDataValidation().Value);
        }

        [Test]
        public void WorksheetClearDataValidation()
        {
            using var emptyWb = new XLWorkbook();
            using var wb = SetupWorkbook(emptyWb);
            var ws = wb.Worksheets.First();

            ws.Clear(XLClearOptions.DataValidation);

            foreach (var c in ws.Range("A1:A3").Cells())
            {
                Assert.IsFalse(c.IsEmpty());
                Assert.AreEqual(backgroundColor, c.Style.Fill.BackgroundColor);
                Assert.AreEqual(foregroundColor, c.Style.Font.FontColor);
                Assert.IsTrue(ws.ConditionalFormats.Any());
                Assert.IsTrue(c.HasComment);
            }

            Assert.AreEqual(XLDataType.Text, ws.Cell("A1").DataType);
            Assert.AreEqual(XLDataType.Text, ws.Cell("A2").DataType);
            Assert.AreEqual(XLDataType.DateTime, ws.Cell("A3").DataType);

            Assert.AreEqual(string.Empty, ws.Cell("A1").GetDataValidation().Value);
        }

        [Test]
        public void DeleteClearedCellValue()
        {
            using var emptyWb = new XLWorkbook();
            using var ms = new MemoryStream();
            using (var wb = SetupWorkbook(emptyWb))
            {
                var ws = wb.Worksheets.First();
                Assert.AreEqual("Hello world!", ws.Cell("A1").GetString());
                Assert.AreEqual(new DateTime(2018, 1, 15), ws.Cell("A3").GetDateTime());

                wb.SaveAs(ms);
            }

            using (var wb = new XLWorkbook(ms))
            {
                var ws = wb.Worksheets.First();
                ws.Clear(XLClearOptions.Contents);
                Assert.AreEqual("", ws.Cell("A1").GetString());
                Assert.Throws<FormatException>(() => ws.Cell("A3").GetDateTime());

                wb.Save();
            }

            using (var wb = new XLWorkbook(ms))
            {
                var ws = wb.Worksheets.First();
                Assert.AreEqual("", ws.Cell("A1").GetString());
                Assert.Throws<FormatException>(() => ws.Cell("A3").GetDateTime());
            }
        }

        [TestCase(XLClearOptions.All, 2)]
        [TestCase(XLClearOptions.AllContents, 4)]
        [TestCase(XLClearOptions.AllFormats, 4)]
        [TestCase(XLClearOptions.Contents, 4)]
        [TestCase(XLClearOptions.MergedRanges, 2)]
        public void CanClearMergedRanges(XLClearOptions options, int expectedCount)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Test");

            ws.Range("A1:C3").Merge();
            ws.Range("A4:B6").Merge();
            ws.Range("D1:F3").Merge();
            ws.Range("E4:F6").Merge();

            ws.Range("C1:D6").Clear(options);

            Assert.AreEqual(expectedCount, ws.MergedRanges.Count);
        }
    }
}
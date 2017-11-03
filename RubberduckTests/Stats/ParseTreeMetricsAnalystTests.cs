﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rubberduck.Navigation.CodeMetrics;
using RubberduckTests.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RubberduckTests.Stats
{
    [TestClass]
    public class ParseTreeMetricsAnalystTests
    {
        private ParseTreeMetricsAnalyst cut;
        private CancellationTokenSource cts;

        [TestInitialize]
        public void Setup()
        {
            cut = new ParseTreeMetricsAnalyst();
            cts = new CancellationTokenSource();
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void EmptyModule_HasMetricsZeroed()
        {
            var code = @"";
            var state = MockParser.ParseString(code, out var qmn);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(new CodeMetricsResult(), metrics.Result);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void EmptySub_HasCyclomaticComplexity_One()
        {
            var code = @"
Sub NoCode()
End Sub
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(1, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void EmptyFunction_HasCyclomaticComplexit_One()
        {
            var code = @"
Function NoCode()
End Function
";

            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(1, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void ModuleHas_AsManyLines_AsPhysicalLines()
        {
            foreach (var lineCount in new int[]{ 0, 10, 15, 200, 1020 })
            {
                var builder = new StringBuilder();
                for (int i = 0; i < lineCount; i++)
                {
                    builder.Append(Environment.NewLine);
                }
                var code = builder.ToString();

                var state = MockParser.ParseString(code, out var _);
                var metric = cut.ModuleMetrics(state, cts.Token).First();
                Assert.AreEqual(lineCount, metric.Result.Lines);
            }
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void SingleIfStatement_HasCyclomaticComplexity_2()
        {
            var code = @"
Sub IfStatement()
    If True Then
    End If
End Sub
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(2, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void SingleIfElseStatement_HasCyclomaticComplexity_2()
        {
            var code = @"
Sub IfElseStatement()
    If True Then
    Else
    End If
End Sub
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(2, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void IfElseIfStatement_HasCyclomaticComplexity_3()
        {
            var code = @"
Sub IfElseifStatement()
    If True Then
    ElseIf False Then
    End If
End Sub
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(3, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void IfElseIfElseStatement_HasCyclomaticComplexity_3()
        {
            var code = @"
Sub IfElseifStatement()
    If True Then
    ElseIf False Then
    Else
    End If
End Sub
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(3, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void NestedIfStatement_HasCyclomaticComplexity_3()
        {
            var code = @"
Sub IfElseifStatement()
    If True Then
        If False Then
        End If
    End If
End Sub
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(3, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void ForeachLoop_HasCyclomaticComplexity_2()
        {
            var code = @"
Sub ForeachLoop(ByRef iterable As Object)
    Dim stuff As Variant
    For Each stuff In iterable 
    Next stuff
End Sub
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(2, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void ForToNextLoop_HasCyclomaticComplexity_2()
        {
            var code = @"
Sub ForToNextLoop(ByVal ubound As Long)
    Dim i As Long
    For i = 0 To ubound Step 1
        ' nothing
    Next i
End Sub
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(2, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void CaseOnlyElse_HasCyclomaticComplexity_1()
        {
            var code = @"
Sub CaseOnlyElse(ByVal number As Long) 
    Select Case number
        Case Else
    End Select
End Sub
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(1, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void CaseBlock_HasCyclomaticComplexity_CorrespondingToCaseLabels()
        {
            foreach (var blockCount in new int[] { 1, 2, 5, 25, 40 })
            {
                var caseBlockBuilder = new StringBuilder();
                for (int i = 0; i < blockCount; i++) {
                    caseBlockBuilder.Append($"\r\n        Case number < {i}\r\n\r\n");
                }
                var code = @"
Sub CaseBlockWithCounts(ByVal number As Long)
    Select Case number
" + caseBlockBuilder.ToString() + @"
        Case Else
    End Select
End Sub
";
                var state = MockParser.ParseString(code, out var _);
                var metrics = cut.ModuleMetrics(state, cts.Token).First();
                Assert.AreEqual(blockCount + 1, metrics.Result.CyclomaticComplexity);
            }
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void PropertyGet_HasCyclomaticComplexity_One()
        {
            var code = @"
Public Property Get Complexity() As Long
    Complexity = 1
End Property
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(1, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void PropertyLet_HasCyclomaticComplexity_One()
        {
            var code = @"
Option Explicit

Private mComplexity As Long

Public Property Let Complexity(ByVal complexity As Long)
    mComplexity = complexity
End Property
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(1, metrics.Result.CyclomaticComplexity);
        }

        [TestMethod]
        [TestCategory("Code Metrics")]
        public void PropertySet_HasCyclomaticComplexity_One()
        {
            var code = @"
Option Explicit

Private mComplexity As Object

Public Property Set Complexity(ByRef complexity As Object)
    mComplexity = complexity
End Property
";
            var state = MockParser.ParseString(code, out var _);
            var metrics = cut.ModuleMetrics(state, cts.Token).First();
            Assert.AreEqual(1, metrics.Result.CyclomaticComplexity);
        }
    }
}

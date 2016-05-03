using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace ClassAsString.Model
{
    internal class ClassBodyToStringConverter
    {
        private readonly DTE2 _dte;

        public ClassBodyToStringConverter(DTE2 dte)
        {
            this._dte = dte;
        }

        public void Execute(object sender, EventArgs e)
        {
            var view = GetCurentTextView();

            if (view != null)
            {
                var txtSel = _dte.ActiveDocument.Selection as TextSelection;

                txtSel.SelectAll();


                var text = txtSel.Text;
                var immutableText = text;

                if (!string.IsNullOrEmpty(text) && CodeRegularExpressions.ClassBodyRegex.IsMatch(text))
                {
                    try
                    {
                        _dte.UndoContext.Open(@"Introduce 'ToCode' field with class body");

                        using (var edit = view.TextBuffer.CreateEdit())
                        {
                            var toDeleteSubStrings = new List<Substring>();
                            var endOfClassIndexes = new List<int>();

                            foreach (Match m in CodeRegularExpressions.ToCodeRegex.Matches(text))
                            {
                                toDeleteSubStrings.Add(new Substring(m.Index + 1, m.Length));
                                    //first char is (;|^|}|{) so we need to leave it alone to not break code
                            }

                            text = CodeRegularExpressions.ToCodeRegex.Replace(text, "$1");

                            /*    while (toCodeRegex.IsMatch(text))
                                {
                                    var m = toCodeRegex.Match(text);
                                    text = text.Remove(m.Index, m.Length);
                                }*/

                            if (!view.Selection.IsEmpty)
                            {
                                //  edit.Delete(view.Selection.SelectedSpans[0].Span);
                                view.Selection.Clear();
                            }

                            foreach (var deleteSubString in toDeleteSubStrings)
                            {
                                edit.Delete(deleteSubString.Index, deleteSubString.Length);
                            }

                            foreach (Match match in CodeRegularExpressions.ClassBodyRegex.Matches(immutableText))
                            {
                                endOfClassIndexes.Add(match.Index + match.Length);
                            }


                            var classBodyMatches = CodeRegularExpressions.ClassBodyRegex.Matches(text);

                            if (classBodyMatches.Count != endOfClassIndexes.Count)
                                throw new Exception("classBodyMatches.Count!=endOfClassIndexes.Count");

                            for (var i = 0; i < classBodyMatches.Count; i++)
                            {
                                var str = classBodyMatches[i].Value.Replace(@"""", @"""""");

                                var startIndex = str.IndexOf('{') + 1;
                                str = str.Substring(startIndex, str.LastIndexOf('}') - startIndex);

                                edit.Insert(endOfClassIndexes[i] - 1,$@"{Environment.NewLine}{Environment.NewLine}public const string ToCode = @""{str}"";{Environment.NewLine}{Environment.NewLine}");
                            }


                            edit.Apply();
                        }
                    }
                    catch (Exception ex)
                    {
                        //Logger.Log(ex);
                    }
                    finally
                    {
                        _dte.UndoContext.Close();
                    }
                }
            }
        }

        private IWpfTextView GetCurentTextView()
        {
            var componentModel = GetComponentModel();
            if (componentModel == null) return null;
            var editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            var nativeView = GetCurrentNativeTextView();

            if (nativeView != null)
                return editorAdapter.GetWpfTextView(nativeView);

            return null;
        }

        public static IVsTextView GetCurrentNativeTextView()
        {
            var textManager = (IVsTextManager) ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));

            IVsTextView activeView = null;
            textManager.GetActiveView(1, null, out activeView);
            return activeView;
        }

        public static IComponentModel GetComponentModel()
        {
            return (IComponentModel) Package.GetGlobalService(typeof(SComponentModel));
        }


        public struct Substring
        {
            public int Index { get; }
            public int Length { get; }

            public Substring(int index, int length)
            {
                Index = index;
                Length = length;
            }
        }
    }
}
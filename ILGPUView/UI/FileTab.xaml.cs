using ILGPUView.Files;
using ILGPUView.Utils;
using Microsoft.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ILGPUView.UI
{
    /// <summary>
    /// Interaction logic for FileTab.xaml
    /// </summary>
    public partial class FileTab : UserControl
    {
        public CodeFile codeFile;
        public string displayedText;
        Action<string> onTextChanged;

        private Regex tokenRegex = new Regex("([ \\t{}()<>:;,])");
        public FileTab(CodeFile codeFile, Action<string> onTextChanged)
        {
            InitializeComponent();

            this.codeFile = codeFile;
            this.onTextChanged = onTextChanged;

            code.TextChanged += Code_TextChanged;
            code.CaretPosition = code.Document.ContentStart;

            SetText(codeFile.fileContents);
        }

        private static HashSet<string> keywords = new HashSet<string>
        {
            "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "double", "float", "decimal",
            "string", "char", "void", "object", "typeof", "sizeof", "null", "true", "false", "if", "else", "while", "for", "foreach", "do", "switch",
            "case", "default", "lock", "try", "throw", "catch", "finally", "goto", "break", "continue", "return", "public", "private", "internal",
            "protected", "static", "readonly", "sealed", "const", "fixed", "stackalloc", "volatile", "new", "override", "abstract", "virtual",
            "event", "extern", "ref", "out", "in", "is", "as", "params", "__arglist", "__makeref", "__reftype", "__refvalue", "this", "base",
            "namespace", "using", "class", "struct", "interface", "enum", "delegate", "checked", "unchecked", "unsafe", "operator", "implicit", "explicit"
        };

        public void SetText(string text)
        {
            code.TextChanged -= Code_TextChanged;
            
            displayedText = text;

            string[] lines = displayedText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            code.Document.Blocks.Clear();
            code.Document.PageWidth = 1600;

            for (int i = 0; i < lines.Length; i++)
            {
                Paragraph p = new Paragraph();

                UpdateParagraph(ref p, lines[i]);

                code.Document.Blocks.Add(p);
            }

            code.TextChanged += Code_TextChanged;
        }
        
        private void UpdateParagraph(ref Paragraph p, string newLine)
        {
            p.Margin = new Thickness(0);

            int firstInstanceOfComment = newLine.Trim().IndexOf("//");

            if (firstInstanceOfComment == 0)
            {
                Run run = new Run();
                run.Foreground = new SolidColorBrush(Color.FromRgb(255, 191, 139));
                run.Text = newLine;
                p.Inlines.Clear();
                p.Inlines.Add(run);
            }
            else
            {
                string[] tokens = tokenRegex.Split(newLine);
                
                p.Inlines.Clear();

                for (int j = 0; j < tokens.Length; j++)
                {
                    Run run = new Run();

                    string toSearch = tokens[j].Trim().ToLower();

                    if (keywords.Contains(toSearch))
                    {
                        run.Foreground = new SolidColorBrush(Color.FromRgb(99, 130, 255));
                    }
                    else if(AssemblyHelpers.getAllTypes().Contains(toSearch))
                    {
                        run.Foreground = new SolidColorBrush(Color.FromRgb(99, 130, 99));
                    }
                    else
                    {
                        run.Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                    }

                    run.Text = tokens[j];

                    p.Inlines.Add(run);
                }
            }
        }

        private void Code_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if(textUpdate == null || textUpdate.Status == DispatcherOperationStatus.Completed)
            //{
            //    textUpdate = Dispatcher.InvokeAsync(() => { SetText(fullText); });
            //}

            //Dispatcher.Invoke(() => { SetText(new TextRange(code.Document.ContentStart, code.Document.ContentEnd).Text); });

            if (onTextChanged != null)
            {
                onTextChanged(displayedText);
            }
        }

        private void code_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.IsDown && e.Key == Key.Tab)
            {
                code.Selection.Start.InsertTextInRun("    ");
                e.Handled = true;
            }
        }
    }
}
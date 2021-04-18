using ILGPUView.Files;
using Markdig;
using Neo.Markdig.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace ILGPUView.UI
{
    /// <summary>
    /// Interaction logic for MarkdownTab.xaml
    /// </summary>
    public partial class MarkdownTab : UserControl
    {
        public CodeFile codeFile;
        public FileTabs parent;

        public MarkdownTab(FileTabs parent, CodeFile codeFile)
        {
            this.codeFile = codeFile;
            this.parent = parent;

            InitializeComponent();

            SetText(codeFile.fileContents);
        }

        public void SetText(string text)
        {
            switch (codeFile.textType)
            {
                case TextType.code:
                    break;
                case TextType.markdown:
                    ParseTextForMarkdown(text);
                    code.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    break;
            }
        }

        private void ParseTextForMarkdown(string text)
        {
            var doc = MarkdownXaml.ToFlowDocument(text,
                new MarkdownPipelineBuilder()
                .UseXamlSupportedExtensions()
                .UseAutoLinks()
                .Build()
            );
            code.Document = doc;
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string link = ((Uri)e.Parameter).ToString();

            if (link.StartsWith("http"))
            {
                Console.WriteLine(link);
            }
            else
            {
                CodeFile file = new CodeFile(link, ".\\Wiki", OutputType.terminal, TextType.markdown);
                if (file.TryLoad())
                {
                    parent.AddCodeFile(file);
                }
            }
        }
    }
}

using ILGPUView.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ILGPUView.Files
{
    public class SampleManager
    {
        public List<CodeFile> loadedFiles;
        public FileTabs fileTabs;

        private Action OnSearchDone;
        private Task searchTask;

        private Dictionary<string, CodeFile> samples;

        public SampleManager(FileTabs fileTabs, Action OnSearchDone)
        {
            this.fileTabs = fileTabs;
            this.OnSearchDone = OnSearchDone;
            loadedFiles = new List<CodeFile>();
            SearchAsync();
        }

        public List<string> getSampleNames()
        {
            if(samples != null && samples.Count > 0)
            {
                return samples.Keys.ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        public void OpenAllSamples()
        {
            foreach(string s in getSampleNames())
            {
                LoadSample(s);
            }
        }

        public void LoadSample(string name)
        {
            if(samples.ContainsKey(name))
            {
                if(samples[name].TryLoad())
                {
                    fileTabs.AddCodeFile(samples[name]);
                }
            }
        }

        private void SearchAsync()
        {
            if(searchTask == null || searchTask.IsCompleted)
            {
                searchTask = Task.Run(() =>
                {
                    try
                    {
                        Dictionary<string, CodeFile> samples = new Dictionary<string, CodeFile>();
                        List<string> directories = new List<string>(Directory.EnumerateDirectories(".\\Samples\\"));

                        for (int i = 0; i < directories.Count; i++)
                        {
                            if (File.Exists(directories[i] + "\\Program.cs"))
                            {
                                CodeFile code = new CodeFile("Program.cs", directories[i], OutputType.terminal, TextType.code);
                                samples.Add(directories[i], code);
                            }
                            else
                            {
                                Console.WriteLine("Unable to load sample in: " + directories[i]);
                            }
                        }

                        this.samples = samples;

                        OnSearchDone();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Searching for Samples Failed\n" + e.ToString());
                    }
                });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILGPUView.Utils
{
    public class Logger : TextWriter
    {
        public static Logger staticInstance = null;

        private readonly int textMaxLength = 10_000_000;
        public string text { get; private set; }

        public Action onUpdate;

        public Logger(Action onUpdate)
        {
            this.onUpdate = onUpdate;
            text = "";
            Console.SetOut(this);
            staticInstance = this;
        }

        public void Save()
        {
            File.WriteAllText(".\\log.txt", text);
        }

        public void clear()
        {
            if(!MainWindow.sampleTestMode)
            {
                text = "";
                update();
            }
        }

        private void update()
        {
            if(text.Length > textMaxLength)
            {
                text = text.Substring(text.Length - textMaxLength);
            }

            if(onUpdate != null)
            {
                onUpdate();
            }
        }

        public override void Write(char value)
        {
            text += value;
            update();
        }

        public override void Write(string value)
        {
            text += value;
            update();
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }
}

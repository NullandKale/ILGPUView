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
        private readonly int textMaxLength = 10_000_000;
        public string text { get; private set; }

        public Action onUpdate;

        public Logger(Action onUpdate)
        {
            this.onUpdate = onUpdate;
            text = "";
            Console.SetOut(this);
        }

        public void clear()
        {
            text = "";
            update();
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

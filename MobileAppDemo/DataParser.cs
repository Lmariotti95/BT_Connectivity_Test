using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MobileAppDemo
{
    public class RxLine
    {
        public string Text {  get; set; }
        public bool Parse {  get; set; }
    }

    public class DataParser
    {
        private static DataParser instance = null;

        private static readonly string EOL_IDENTIFIER = "\r\n";
        private static readonly int LINE_BUFF_SIZE = 64;

        private List<BtCommand> commands = new List<BtCommand>();

        private static List<string> payload = new List<string>();

        // Buffer di ricezione gestito in modo circolare
        private static RxLine[] rawLine = new RxLine[LINE_BUFF_SIZE];

        // Riga attuale
        private static int fillLine = 0;    

        // Costruttore privato per Singleton
        private DataParser() { }

        public static DataParser Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DataParser();

                    for(int i = 0; i < rawLine.Length; i++) 
                    {
                        rawLine[i] = new RxLine();
                    }

                    // La prima volta che viene creata l'istanza lancio anche il thread 
                    Thread t = new Thread(() => Parser())
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    t.Start();
                }

                return instance;
            }
        }

        public void SetCommand(List<BtCommand> btCommands)
        {
            if (btCommands == null)
                return;

            if(commands == null)
                commands = new List<BtCommand>();

            commands.Clear();
            commands.AddRange(btCommands);
        }

        private static void Parser()
        {
            int parsingLine = 0;
            while (true)
            {
                // Se è richiesto di controllare l'i-esima riga
                if(rawLine[parsingLine].Parse)
                {
                    // Verifico che contenga il terminatore
                    if (rawLine[parsingLine].Text.Contains(EOL_IDENTIFIER))
                    {
                        // Se ho dei comandi configurati
                        if (instance.commands != null)
                        {
                            // Verifico se la linea ricevuta è un comando e nel caso ne lancio la relativa callback
                            foreach (BtCommand cmd in instance.commands)
                            {
                                if (rawLine[parsingLine].Text.Contains(cmd.Text))
                                {
                                    cmd.Callback?.Invoke();

                                    rawLine[parsingLine].Text = "";

                                    // Brutto ma funzionale
                                    continue;
                                }
                            }
                        }

                        // Se la riga non era un comando allora l'aggiungo al resto delle righe "payload" 
                        payload.Add(rawLine[parsingLine].Text);

                        // Resetto la riga i-esima
                        rawLine[parsingLine].Text = "";
                    }

                    // Resetto la richiesta di controlli sulla riga i-esima
                    rawLine[parsingLine].Parse = false;

                    // Mi sposto in modo circolare alla prossima riga da controllare
                    parsingLine = (++parsingLine) % LINE_BUFF_SIZE;
                }
            }
        }

        public void PushCharacter(char ch)
        {
            // Si può ottimizzare con un buffer fisso di 
            rawLine[fillLine].Text += ch;

            // Quando ricevo il terminatore 
            if (ch == '\n')
            {
                // Segnalo che posso controllare la riga i-esima
                rawLine[fillLine].Parse = true;

                // Mi sposto in modo circolare alla prossima riga da riempire
                fillLine = (++fillLine) % LINE_BUFF_SIZE;
            }
        }

        public List<string> GetPayload()
        {
            return payload;
        }

        public void ClrPayload()
        {
            payload.Clear();

            for(int i = 0; i < rawLine.Length; i++)
            {
                rawLine[i].Text = "";
                rawLine[i].Parse = false;
            }

            fillLine = 0;
        }    
    }
}

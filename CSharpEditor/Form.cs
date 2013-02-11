using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;

namespace CSharpEditor
{
    public partial class CSharpEditorForm : Form
    {
        private ListBox listBoxAutoComplete;
        private ToolStripStatusLabel toolStripStatusLabel = new ToolStripStatusLabel();
        private Point caretPos;
        private static int _counter;
         
        public CSharpEditorForm()
        {
            InitializeComponent();
            // Instantiate listBoxAutoComplete object
            listBoxAutoComplete = new ListBox();
            // Add the ListBox to the form 
            this.Controls.Add(listBoxAutoComplete);
            // Add status bar
            statusStrip.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel });
            defineButtonEvents();
        }

        [DllImport("Kernel32.dll")]
        static extern Boolean AllocConsole();

        private void CSharpEditorForm_Load(object sender, EventArgs e)
        {
            if (!AllocConsole())
                MessageBox.Show("Failed to alloc console");
        }

        private void editorPane_KeyDown(object sender, KeyEventArgs e)
        {
            Clear();
            StatusLine();

            // Detecting the dot key
            if (e.KeyData == Keys.OemPeriod)
            {
                // Get current line
                int currentLineIndex = editorPane.GetLineFromCharIndex(editorPane.SelectionStart);

                string currentLine;
                try
                {
                    currentLine = this.editorPane.Lines[currentLineIndex];
                }
                catch(IndexOutOfRangeException)
                {
                    return;
                }

                if (currentLine==null || currentLine[currentLine.Length-1] == '.')
                    return;

                Console.WriteLine("Current line numb.: {0}, {1}", currentLineIndex + 1, currentLine);

                String[] wordsInLine = currentLine.Split('.');
                Type typeOfAWord;

                try
                {
                    typeOfAWord = Type.GetType("System." + wordsInLine[wordsInLine.Length - 1]);
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Argumento null");
                    return;
                }
                if (typeOfAWord == null) return;

                if (!this.listBoxAutoComplete.Visible)
                {
                    this.listBoxAutoComplete.Items.Clear();
                    // Populate the Auto Complete list box
                    //this.listBoxAutoComplete.Items.Add("Olá " + ++_counter);

                    foreach (MethodInfo mInfo in typeOfAWord.GetMethods())
                    {
                        this.listBoxAutoComplete.Items.Add(mInfo.Name);
                    }
                    // Display the Auto Complete list box
                    DisplayAutoCompleteList();
                }
            }
        }
        

        // Display the Auto Complete list box
        private void DisplayAutoCompleteList()
        {
            // Find the position of the caret
            Point caretLoc = this.editorPane.GetPositionFromCharIndex(editorPane.SelectionStart);
            caretLoc.Y += (int)Math.Ceiling(this.editorPane.Font.GetHeight()) * 2 + 13;
            caretLoc.X += 20;
            this.listBoxAutoComplete.Location = caretLoc;
            this.listBoxAutoComplete.Height = 100;
            this.listBoxAutoComplete.Width = 160;
            this.listBoxAutoComplete.BringToFront();
            this.listBoxAutoComplete.Show();
        }
        private void StatusLine()
        {
            int ln = this.editorPane.GetLineFromCharIndex(editorPane.SelectionStart);
            int cn = (editorPane.SelectionStart) - (editorPane.GetFirstCharIndexFromLine(ln)) + 1;
            ln = ln + 1;
            caretPos.X = cn;
            caretPos.Y = ln;
            string lnColString = "Ln: " + ln.ToString() + " Col: " + cn.ToString();
            statusStrip.Items[0].Text = lnColString;
        }
        private void Clear()
            {
                this.listBoxAutoComplete.Hide();
        }
        private void editorPane_MouseClick(object sender, MouseEventArgs e)
        {
            StatusLine();
        }

        private void editorPane_MouseUp(object sender, MouseEventArgs e)
        {
            StatusLine();
        }

        private void editorPane_TextChanged(object sender, EventArgs e)
        {
            StatusLine();
        }

        private void saveFileButton_Click(object sender, EventArgs e)
        {
            editorPane.SaveFile();

            Console.WriteLine("save");
        }

        private void loadFileButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("load");
        }

        private void compileButton_click(object sender, EventArgs e)
        {
            Assembly ass=Assembly.GetAssembly(editorPane.GetType());
            
            Console.WriteLine("compileButton");
        }

        private void runButton_click(object sender, EventArgs e)
        {
            Console.WriteLine("runButton");
        }

        private void addAssemblyRefButton_click(object sender, EventArgs e)
        {
            Console.WriteLine("addAssemblyRefButton");
        }


        private void removeAssemblyRefButton_click(object sender, EventArgs e)
        {
            Console.WriteLine("removeAssemblyRefButton");
        }

        private void newFileButton_click(object sender, EventArgs e)
        {
            Console.WriteLine("newFileButton");
        }

        private void defineButtonEvents()
        {
            loadFileButton.Click += new System.EventHandler(this.loadFileButton_Click);
            saveFileButton.Click += new System.EventHandler(this.saveFileButton_Click);
            removeAssemblyRefButton.Click += new System.EventHandler(this.removeAssemblyRefButton_click);
            addAssemblyRefButton.Click += new System.EventHandler(this.addAssemblyRefButton_click);
            runButton.Click += new System.EventHandler(this.runButton_click);
            compileButton.Click += new System.EventHandler(this.compileButton_click);
            newFileButton.Click += new System.EventHandler(this.newFileButton_click);
        }
    }
}

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CSharp;

namespace CSharpEditor
{
    public partial class CSharpEditorForm : Form
    {
        private ListBox listBoxAutoComplete;
        private ToolStripStatusLabel toolStripStatusLabel = new ToolStripStatusLabel();
        private Point caretPos;
        private static int _counter;
        private String lastLocationSaved = null;
        private String lastLocationCompiled = null;
        private Dictionary<String, Object> variableTypesInfo = new Dictionary<String, Object>();

        List<string> referencedAssemblies = new List<string>();


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
            String line;
            getCurrentLine(out line);
            Keys key = e.KeyData;


            // Detecting the dot key
            if (key == Keys.OemPeriod)
                displayAutoCompleteBox(line);
            else
                if (key == Keys.Oemcomma)
                {
                    String[] s = line.Split('=');
                    getTypeAndName(s[0]);
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
            String line;
            getCurrentLine(out line);
            if (line.Length > 0 && line[line.Length - 1] == ';')
            {
                String[] s = line.Split('=');
                getTypeAndName(s[0]);
            }

        }

        private void saveFileButton_Click(object sender, EventArgs e)
        {

            saveFileDialog1.FileName = "";
            saveFileDialog1.Filter = "cs files (*.cs)|*.cs|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                lastLocationSaved = saveFileDialog1.FileName;
                using (Stream fs = saveFileDialog1.OpenFile())
                {
                    editorPane.SaveFile(fs, RichTextBoxStreamType.PlainText);
                }
            }
            Console.WriteLine("save");
        }

        private void loadFileButton_Click(object sender, EventArgs e)
        {

            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "cs files (*.cs)|*.cs|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                lastLocationSaved = openFileDialog1.FileName;
                using (Stream fs = openFileDialog1.OpenFile())
                {
                    editorPane.LoadFile(fs, RichTextBoxStreamType.PlainText);
                }
            }
            Console.WriteLine("load");
        }

        private void compileButton_click(object sender, EventArgs e)
        {

            //Assembly ass = Assembly.GetAssembly(editorPane.GetType());
            CodeDomProvider codeProvider = new CSharpCodeProvider();
            string sourceCode = editorPane.Text;
            string sourceFile = null;
            string[] resourceFiles = null;
            string[] refAssemblies = referencedAssemblies.ToArray();

            string errors = null;
            CompilerResults compilerResults;
            
            if (lastLocationSaved != null)
            {
                bool comp;
                using (TextReader fs = new StreamReader(lastLocationSaved,System.Text.Encoding.Default))
                {
                    string aux = fs.ReadToEnd();
                    string aux2 = editorPane.Text;
                    aux = Regex.Replace(aux, @"\s", "");
                    aux2 = Regex.Replace(aux2, @"\s", "");
                    aux = Regex.Replace(aux, @"\r", "");
                    aux2 = Regex.Replace(aux2, @"\r", "");
                    comp = aux.Equals(aux2);
                }

                if (comp)
                {
                    int i = editorPane.Text.IndexOf("Main");
                    if (i != -1) //existe metodo main
                    {

                        string exeFile = lastLocationSaved.Replace("cs", "exe");


                        if (CompilerServices.Compiler.CompileCode(codeProvider, sourceCode, sourceFile, exeFile,
                                                                  null, resourceFiles, refAssemblies, out errors,
                                                                  out compilerResults))
                        {
                            lastLocationCompiled = exeFile;
                            errorsList.Clear();
                            errorsList.AppendText("Compilado com sucesso");
                        }
                        else
                        {
                            errorsList.Clear();
                            errorsList.AppendText(errors);
                        }
                    }
                    else
                    {
                        string assemblyName = lastLocationSaved.Replace("cs", "dll"); ;


                        if (CompilerServices.Compiler.CompileCode(codeProvider, sourceCode, sourceFile, null,
                                                                  assemblyName, resourceFiles, refAssemblies, out errors,
                                                                  out compilerResults))
                        {
                            lastLocationCompiled = assemblyName;
                            errorsList.Clear();
                            errorsList.AppendText("Compilado com sucesso");
                        }

                    }
                }
                else
                {
                    errorsList.Clear();
                    errorsList.AppendText("Guarde o Ficheiro Primeiro!");
                }

            }
            else
            {
                errorsList.Clear();
                errorsList.AppendText("Guarde o Ficheiro Primeiro!");
            }

            Console.WriteLine("compileButton");
        }

        private void runButton_click(object sender, EventArgs e)
        {

            if (lastLocationCompiled != null && lastLocationCompiled.IndexOf("exe") != -1)
            {
                var proc = new Process();
                proc.StartInfo.FileName = lastLocationCompiled;
                proc.Start();

                errorsList.Clear();
                errorsList.AppendText("Programa Executado");

                proc.WaitForExit();
                var exitCode = proc.ExitCode;
                proc.Close();
                Console.WriteLine("runButton");
            }
            else
            {
                errorsList.Clear();
                errorsList.AppendText("O ficheiro destino não pode ser executado\n (é um ficheiro dll?)");
            }
        }

        private void addAssemblyRefButton_click(object sender, EventArgs e)
        {

            openFileDialog2.FileName = "";
            openFileDialog2.Filter = "dll files (*.dll)|*.dll|All files (*.*)|*.*";
            openFileDialog2.Multiselect = true;
            openFileDialog2.ShowDialog();

            foreach (string fileName in openFileDialog2.FileNames)
            {
                if (referencedAssemblies.IndexOf(fileName)==-1)
                {
                    referencedAssemblies.Add(fileName);
                    assemblyRefsComboBox.Items.Add(fileName.Substring(fileName.LastIndexOf("\\")));
                }
            }


            Console.WriteLine("addAssemblyRefButton");
        }

        private void removeAssemblyRefButton_click(object sender, EventArgs e)
        {
            
            var selected = assemblyRefsComboBox.SelectedItem;
            if (selected != null)
            {

                foreach (var path in referencedAssemblies)
                {
                    if (path.IndexOf(selected.ToString())!=-1)
                    {
                        referencedAssemblies.Remove(path);
                        assemblyRefsComboBox.Items.Remove(selected);
                        assemblyRefsComboBox.Text = "";
                        break;
                    }
                }
                
            }
            Console.WriteLine("removeAssemblyRefButton");
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            referencedAssemblies.Clear();
            assemblyRefsComboBox.Items.Clear();
            assemblyRefsComboBox.Text = "";
        }

        private void newFileButton_click(object sender, EventArgs e)
        {
            lastLocationSaved = null;
            lastLocationCompiled = null;
            editorPane.Clear();
            Console.WriteLine("newFileButton");
        }

        private void listBoxAutoComplete_Click(object sender, EventArgs e)
        {
            int index = listBoxAutoComplete.SelectedIndex;
            int i = editorPane.GetLineFromCharIndex(editorPane.SelectionStart);
            editorPane.AppendText(listBoxAutoComplete.Items[index].ToString());
            Clear();
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
            listBoxAutoComplete.Click += new System.EventHandler(this.listBoxAutoComplete_Click);
        }

        private void getCurrentLine(out String line)
        {
            int currentLineIndex = editorPane.GetLineFromCharIndex(editorPane.SelectionStart);

            try
            {
                line = this.editorPane.Lines[currentLineIndex];
            }
            catch (IndexOutOfRangeException)
            {
                line = " ";
                return;
            }
        }

        private void displayAutoCompleteBox(String currentLine)
        {
            // Get current line


            if (currentLine == null || currentLine[currentLine.Length - 1] == '.')
                return;

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

        private void getTypeAndName(String typeAndWord)
        {
            String[] x = typeAndWord.Split(' ');

            if (x.Length > 1)
            {
                variableTypesInfo.Add(x[1].TrimEnd(' ', ';'), Type.GetType(x[0]));

                Console.WriteLine("Type - " + x[0]);
                Console.WriteLine("Name - " + x[1].TrimEnd(' ', ';'));

                //Console.WriteLine(variableTypesInfo.
            }

        }
        
    }
}

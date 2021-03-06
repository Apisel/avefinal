﻿using System;
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
        private String lastLocationSaved = null;
        private String lastLocationCompiled = null;
        private String lastLocationImplicitCompiled = null;
        private String currentExecutableDirectory = Directory.GetCurrentDirectory();
        private AppDomain ap;
        private bool isStatic;
        private InstrospectiveClass ic;
        private Dictionary<String, String> variableTypesInfo = new Dictionary<String, String>();
        private List<string> referencedAssemblies = new List<string>();
        private bool isLoading = false;
        
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
            this.listBoxAutoComplete.KeyDown += listBox_EnterKey;

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
            {
                displayAutoCompleteBox(line);
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
            this.listBoxAutoComplete.Height = 120;
            this.listBoxAutoComplete.Width = 230;
            this.listBoxAutoComplete.BringToFront();
            this.listBoxAutoComplete.Show();
            try
            {
                this.listBoxAutoComplete.Select();
                this.listBoxAutoComplete.SetSelected(0, true);
            }
            catch { }


        }

        private void listBox_EnterKey(object sender, KeyEventArgs e)
        {
            if (this.listBoxAutoComplete.Visible)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    writeFromListBox();
                    e.Handled = true;
                }

            }
        }

        private void writeFromListBox()
        {
            int cursorPos = editorPane.SelectionStart;
            int index = listBoxAutoComplete.SelectedIndex;
            int i = editorPane.GetLineFromCharIndex(editorPane.SelectionStart);
            int subS = listBoxAutoComplete.Items[index].ToString().LastIndexOf('.');
            String toAdd = listBoxAutoComplete.Items[index].ToString().Substring(subS + 1).Trim();
            

            if ((subS = toAdd.IndexOf(" ")) != -1)
            {                
                toAdd = toAdd.Substring(subS+1);
                
            }
            toAdd = Regex.Replace(toAdd, " ", "");
            
            editorPane.Text = editorPane.Text.Insert(editorPane.SelectionStart, toAdd);
            editorPane.SelectionStart = cursorPos + toAdd.Length;
            Clear();
        }

        private void StatusLine()
        {
            //TODO
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

            if (!isLoading)
            {
                String line;
                getCurrentLine(out line);
                if (line.Length > 0 && line[line.Length - 1] == ';')
                {
                    line = line.Trim();
                    String[] s = line.Split('=');
                    s[0] = s[0].Trim();
                    s = s[0].Split(' ');
                    //getTypeAndName((s.Length > 2) ? s[1] + " " + s[2] : s[0] + " " + s[1]);
                    if(s.Length>1)//no caso de nao ser metodo
                    getTypeAndName(s[s.Length-2] + " " + s[s.Length-1]);
                }
            }
            else
            {
                foreach (var line in editorPane.Lines)
                {
                    if (line.Length > 0 && line[line.Length - 1] == ';')
                    {

                        String[] s = line.Split('=');
                        if (s.Length > 1)//no caso de ser metodo
                        {
                            s = s[0].Trim().Split(' ');
                            //getTypeAndName((s.Length > 2) ? s[1] + " " + s[2] : s[0] + " " + s[1]);
                            getTypeAndName(s[s.Length - 2] + " " + s[s.Length - 1]);
                        }
                    }

                }


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
                statusStrip.Items[0].Text = "Ficheiro salvo com sucesso";
            }
            else statusStrip.Items[0].Text = "Salvamento abortado";
            Console.WriteLine("save");
        }

        private void loadFileButton_Click(object sender, EventArgs e)
        {

            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "cs files (*.cs)|*.cs|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                isLoading = true;
                lastLocationSaved = openFileDialog1.FileName;
                using (Stream fs = openFileDialog1.OpenFile())
                {
                    editorPane.LoadFile(fs, RichTextBoxStreamType.PlainText);
                }
                isLoading = false;
                statusStrip.Items[0].Text = "Ficheiro carregado com sucesso";
            }
            else statusStrip.Items[0].Text = "Carregamento abortado";

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
                using (TextReader fs = new StreamReader(lastLocationSaved, System.Text.Encoding.Default))
                {
                    string aux = fs.ReadToEnd();
                    string aux2 = sourceCode;
                    aux = Regex.Replace(aux, @"\s", "");
                    aux2 = Regex.Replace(aux2, @"\s", "");
                    aux = Regex.Replace(aux, @"\r", "");
                    aux2 = Regex.Replace(aux2, @"\r", "");
                    comp = aux.Equals(aux2);
                }

                if (comp)
                {
                    int i = sourceCode.IndexOf("Main");
                    if (i != -1) //existe metodo main
                    {

                        string exeFile = lastLocationSaved.Replace(".cs", ".exe");


                        if (CompilerServices.Compiler.CompileCode(codeProvider, sourceCode, sourceFile, exeFile,
                                                                  null, resourceFiles, refAssemblies, out errors,
                                                                  out compilerResults))
                        {
                            lastLocationCompiled = exeFile;

                            statusStrip.Items[0].Text = "Compilado com sucesso";
                            errorsList.Clear();
                        }
                        else
                        {
                            statusStrip.Items[0].Text = "Falha na Compilação";
                            errorsList.AppendText(errors);
                        }
                    }
                    else
                    {
                        string assemblyName = lastLocationSaved.Replace(".cs", ".dll"); ;


                        if (CompilerServices.Compiler.CompileCode(codeProvider, sourceCode, sourceFile, null,
                                                                  assemblyName, resourceFiles, refAssemblies, out errors,
                                                                  out compilerResults))
                        {
                            lastLocationCompiled = assemblyName;

                            statusStrip.Items[0].Text = "Compilado com sucesso";
                            errorsList.Clear();
                        }
                        else
                        {
                            statusStrip.Items[0].Text = "Falha na Compilação";
                            errorsList.AppendText(errors);
                        }
                    }
                }
                else
                {
                    statusStrip.Items[0].Text = "Guarde o Ficheiro Primeiro!";
                }

            }
            else
            {
                statusStrip.Items[0].Text = "Guarde o Ficheiro Primeiro!";
            }
            Console.WriteLine("compileButton");
        }

        private void implicitCompilation(String sourceCode)
        {
            CodeDomProvider codeProvider = new CSharpCodeProvider();
            string errors = null;
            CompilerResults compilerResults;

            string tempLocation = Directory.GetCurrentDirectory() + @"\tempFile";

            int i = sourceCode.IndexOf("Main");
            if (i != -1) //existe metodo main
            {
                string exeFile = tempLocation + ".exe";
                if (CompilerServices.Compiler.CompileCode(codeProvider, sourceCode, null,
                    exeFile, null, null, referencedAssemblies.ToArray(),
                    out errors, out compilerResults)) lastLocationImplicitCompiled = exeFile;

            }

            else
            {
                string library = tempLocation + ".dll";


                if (CompilerServices.Compiler.CompileCode(codeProvider, sourceCode, null, null,
                                                              library, null, referencedAssemblies.ToArray(), out errors,
                                                              out compilerResults)) lastLocationImplicitCompiled = library;

            }
        }

        private void runButton_click(object sender, EventArgs e)
        {

            if (lastLocationCompiled != null && lastLocationCompiled.IndexOf("exe") != -1)
            {
                var proc = new Process();
                proc.StartInfo.FileName = lastLocationCompiled;
                proc.Start();

                statusStrip.Items[0].Text = "Programa Executado";

                proc.WaitForExit();
                var exitCode = proc.ExitCode;
                proc.Close();
                Console.WriteLine("runButton");
            }
            else
            {
                statusStrip.Items[0].Text = "O ficheiro destino não pode ser executado\n (é um ficheiro dll?)";
            }
        }

        private void addAssemblyRefButton_click(object sender, EventArgs e)
        {

            openFileDialog2.FileName = "";
            openFileDialog2.Filter = "dll files (*.dll)|*.dll|All files (*.*)|*.*";
            openFileDialog2.Multiselect = true;

            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                foreach (string fileName in openFileDialog2.FileNames)
                {
                    if (referencedAssemblies.IndexOf(fileName) == -1)
                    {
                        referencedAssemblies.Add(fileName);
                        assemblyRefsComboBox.Items.Add(fileName.Substring(fileName.LastIndexOf("\\")));
                    }
                }
                statusStrip.Items[0].Text = "Bibliotecas carregadas com sucesso";
            }
            else statusStrip.Items[0].Text = "Carregamento abortado";

            Console.WriteLine("addAssemblyRefButton");
        }

        private void removeAssemblyRefButton_click(object sender, EventArgs e)
        {

            var selected = assemblyRefsComboBox.SelectedItem;
            if (selected != null)
            {

                foreach (var path in referencedAssemblies)
                {
                    if (path.IndexOf(selected.ToString()) != -1)
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
            clearDictionary();
            errorsList.Clear();
            statusStrip.Items[0].Text = "Código limpo";
            Console.WriteLine("newFileButton");
            if(listBoxAutoComplete.Visible)
            {
                Clear();
                listBoxAutoComplete.Hide();

            }
        }

        private void listBoxAutoComplete_Click(object sender, EventArgs e)
        {
            writeFromListBox();
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
                       
            if (currentLine == null)
                return;
                        
            String[] wordsInLine = currentLine.Split('.');
            String fullTypeOfAWord;
            String unknown = wordsInLine[wordsInLine.Length - 1].Trim();
            int i;
            if ((i = unknown.IndexOf("=")) != -1)
            {
                unknown = unknown.Substring(i + 1);
                unknown = unknown.Trim();
            }
            //ver para variaveis ou constantes
            if (variableTypesInfo.ContainsKey(unknown))
            {
                isStatic = false;
                fullTypeOfAWord = variableTypesInfo[unknown];//(Type)t;
            }
            else
            {
                try
                {
                    isStatic = true;
                    fullTypeOfAWord = getFullNameOfType(unknown); // ve os using e o assemblies
                    //Ver se é de um tipo definido no editorPane
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Argumento null");
                    return;
                }
            }

            //ver para parametros
            String temp;
            if ((temp = getParameterFromAssemblies(unknown))!=null)
                fullTypeOfAWord=temp;

            if (fullTypeOfAWord == null) return;


            //ver para tipos são/definidos tipos primitivos, em namespaces ou bibliotecas
            if (!this.listBoxAutoComplete.Visible) 
            {

                this.listBoxAutoComplete.Items.Clear();
                // Populate the Auto Complete list box
                
                Type type;
                if ((type = Type.GetType(fullTypeOfAWord)) == null)
                {
                    String aux = getNameSpace() + fullTypeOfAWord;
                    if (verifyIfTypeIsDefinedOnAssemblyReferences(aux))
                        addToListBoxAutoCompleteRef(ic, aux);

                }
                else
                {
                    addToListBoxAutoComplete(ic, type);
                }


                if (ap != null)
                    unloadAppDomain();

                // Display the Auto Complete list box
                DisplayAutoCompleteList();
            }
        }

        private void getTypeAndName(String typeAndWord)
        {
            //Assumindo que typeAndWord é o tipo - "TypeName" "refName"
            String[] x = typeAndWord.Split(' ');
            String type;
            //Console.WriteLine(typeAndWord);

            if (x.Length > 1)
            {
                String key = x[1].TrimEnd(' ', ';');
                if (!variableTypesInfo.ContainsKey(key))
                {
                    if (!x[0].Equals("using"))
                        if ((type = getFullNameOfType(x[0].Trim())) != null)
                            variableTypesInfo.Add(key, type);
                }
            }
        }

        private String getFullNameOfType(String type)
        {
            String fullName = getKeywordsType(type);

            if (fullName == null)
            {
                fullName = getFullNameFromUsings(type);
                if (fullName == null && verifyIfTypeIsDefinedOnAssemblyReferences(getNameSpace() + type))
                    fullName = type;
            }

            return fullName;
        }

        private String getNameSpace()
        {
            String namespacer = "";
            Match match = Regex.Match(editorPane.Text, "namespace +[A-Za-z0-9]+");

            if (match.Success)
                namespacer = Regex.Replace(match.Value, "namespace +", "");
            return namespacer + ".";
        }
        
        private String getKeywordsType(String type)
        {
            switch (type)
            {
                case "int": return typeof(int).FullName;
                case "float": return typeof(float).FullName;
                case "double": return typeof(double).FullName;
                case "long": return typeof(long).FullName;
                case "bool": return typeof(bool).FullName;
                case "char": return typeof(char).FullName;
                case "uint": return typeof(uint).FullName;
                case "ulong": return typeof(ulong).FullName;
                case "object": return typeof(object).FullName;
                case "string": return typeof(string).FullName;
                case "byte": return typeof(byte).FullName;
                case "short": return typeof(short).FullName;
            }
            return null;
        }
        
        private String getFullNameFromUsings(String type)
        {
            //percorrer os using para ver se é um tipo de lá definido
            Match match = Regex.Match(editorPane.Text, "using *[A-Za-z1-9.]*");
            String toCompare;

            while (match.Success)
            {
                toCompare = Regex.Replace(match.Value, "using *", "");
                toCompare = toCompare.TrimEnd(' ', ';');
                String aux = toCompare + "." + type;
                if (Type.GetType(aux) != null)
                {
                    return aux;
                }

                match = match.NextMatch();
            }
            return null;
        }
        
        private String removeCurrentLine()
        {
            int currentLineIndex = editorPane.GetLineFromCharIndex(editorPane.SelectionStart);
            String editorText = editorPane.Text;
            String line = editorPane.Lines[currentLineIndex];
            editorText = editorText.Replace(line, "");

            //editorPane.Lines[currentLineIndex] = "";

            //editorPane.Lines[currentLineIndex] = line;

            return editorText;
        }

        //DON'T FORGET TO DO THIS FOR EACH verifyIfTypeIsDefinedOnAssemblyReferences()
        private void unloadAppDomain()
        {
            AppDomain.Unload(ap);
            ap = null;
        }

        private void clearDictionary()
        {
            variableTypesInfo.Clear();
        }

        private bool verifyIfTypeIsDefinedOnAssemblyReferences(String type)
        {
            if (isLoading) implicitCompilation(editorPane.Text);
            else implicitCompilation(removeCurrentLine());

            var setup = new AppDomainSetup
            {
                ApplicationName = "secondApp",
                ApplicationBase = currentExecutableDirectory,
                ShadowCopyFiles = "true",
                ShadowCopyDirectories = currentExecutableDirectory,
                //CachePath = Directory.GetCurrentDirectory()
            };

            //ic = new InstrospectiveClass();
            ap = AppDomain.CreateDomain("compileDomain", null, setup);

            ic = (InstrospectiveClass)ap.CreateInstanceAndUnwrap(
            Assembly.GetExecutingAssembly().FullName, "CSharpEditor.InstrospectiveClass");
            
            foreach (String s in referencedAssemblies) 
            {
                ic.loadAssembly(s);

                if (ic.checkType(type))
                    return true;
                
            }

            //verificar tipos definidos no proprio editorPane;
            DirectoryInfo dirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            foreach (var file in dirInfo.GetFiles("*.exe"))
            {
                if(file.Name.Equals("tempFile.exe"))
                {
                    ic.loadAssembly(file.FullName);
                    if (ic.checkType(type))
                        return true;
                }
            }

            return false;

        }

        private String getParameterFromAssemblies(String param)
        {
            
            foreach (String s in referencedAssemblies)
            {
                ic.loadAssembly(s);
                String aux;
                if ((aux = ic.checkMethodParameters(param))!=null)
                    return aux;

            }
            
            DirectoryInfo dirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            foreach (var file in dirInfo.GetFiles("*.exe"))
            {
                if (file.Name.Equals("tempFile.exe"))
                {
                    ic.loadAssembly(file.FullName);
                    String aux;
                    if ((aux = ic.checkMethodParameters(param)) != null)
                        return aux;
                }
            }
            
            return null;
        }

        private void addToListBoxAutoCompleteRef(InstrospectiveClass ic, String toExtractMembers)
        {
            if (ic == null)
                ic = new InstrospectiveClass();

            foreach (String s in ic.getMembersByString(toExtractMembers, isStatic))
                this.listBoxAutoComplete.Items.Add(s);
           
        }

        private void addToListBoxAutoComplete(InstrospectiveClass ic, Type toExtractMembers)
        {
            if (ic == null)
                ic = new InstrospectiveClass();

            foreach (String s in ic.getMembers(toExtractMembers, isStatic))
                this.listBoxAutoComplete.Items.Add(s);

        }
    }
}

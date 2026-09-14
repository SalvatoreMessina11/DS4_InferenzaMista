using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

partial class DS4Launcher {
    ComboBox frontend=new ComboBox(), network=new ComboBox(), contextPreset=new ComboBox();
    NumericUpDown context=new NumericUpDown(), layers=new NumericUpDown(), maxOutput=new NumericUpDown();
    CheckBox customSplit=new CheckBox();
    Button configurePi=new Button(), preview=new Button();
    Label contextNote=new Label();
    Process piConsole;
    bool changing;
    bool piStarting, stopping;
    int OutputValue {get{return (int)maxOutput.Value;}}
    int ContextValue {get{return (int)context.Value;}}
    bool PiMode {get{return frontend.SelectedIndex==1 && role.SelectedIndex!=1;}}

    void InitAdvancedControls(){
        frontend.DropDownStyle=network.DropDownStyle=contextPreset.DropDownStyle=ComboBoxStyle.DropDownList;
        frontend.Items.AddRange(new object[]{"Chat LLM (solo modello)","LLM + Pi Agent (strumenti)"});frontend.SelectedIndex=1;
        network.Items.AddRange(new object[]{"Wi-Fi / LAN esistente","Ethernet diretto"});network.SelectedIndex=0;
        Row("Modalita di utilizzo",frontend,140);Row("Connessione tra i PC",network,182);
        maxOutput.Minimum=1;maxOutput.Maximum=1000000;maxOutput.Value=32768;
        context.Minimum=2048;context.Maximum=1000000;context.Value=100000;context.ThousandsSeparator=true;
        contextPreset.Items.AddRange(new object[]{"100K","300K","Personalizzato"});contextPreset.SelectedIndex=0;
        Row("Context (uguale sui due PC)",contextPreset,266);contextPreset.Width=180;
        context.SetBounds(510,266,279,31);Controls.Add(context);
        contextNote.SetBounds(26,302,763,33);Controls.Add(contextNote);
        customSplit.Text="Split personalizzato: livelli coordinatore";customSplit.SetBounds(26,342,440,30);Controls.Add(customSplit);
        layers.Minimum=1;layers.Maximum=42;layers.Value=24;layers.SetBounds(510,342,279,31);Controls.Add(layers);
        configurePi.Text="Aiuto Pi";configurePi.SetBounds(292,729,140,38);Controls.Add(configurePi);
        preview.Text="Dettagli avvio";preview.SetBounds(440,729,123,38);Controls.Add(preview);
        contextPreset.SelectedIndexChanged+=delegate{
            if(changing)return;int value;
            if(int.TryParse(Convert.ToString(contextPreset.SelectedItem),out value))context.Value=value;
        };
        context.ValueChanged+=delegate{if(!changing){RefreshContextPresets();Summary();}};
        frontend.SelectedIndexChanged+=delegate{Summary();};network.SelectedIndexChanged+=delegate{Summary();};
        customSplit.CheckedChanged+=delegate{Summary();};layers.ValueChanged+=delegate{Summary();};
        configurePi.Click+=delegate{MessageBox.Show(this,"Scegli un modello e LLM + Pi Agent, poi premi Avvia. La configurazione e automatica. Si aprono due terminali: server LLM e Pi; Pi attende il caricamento. Gli strumenti operano nell'utente Ubuntu selezionato.","Come usare Pi Agent",MessageBoxButtons.OK,MessageBoxIcon.Information);};
        preview.Click+=delegate{try{ValidateFields();MessageBox.Show(this,"Parametri che Avvia eseguira. Questa finestra non avvia il modello.\r\n\r\n"+MemoryEnvironment()+ModelCommand(),"Dettagli tecnici dell'avvio",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception ex){Error(ex);}};

    }
    void InitOutputControls(){
        foreach(Control c in Controls)if(c.Top>=342)c.Top+=82;
        Row("Output massimo (token per risposta)",maxOutput,340);maxOutput.ThousandsSeparator=true;
        Controls.Add(new Label{Text="Il context include prompt e risposta. Output massimo limita ogni risposta; puo finire prima con EOS.",Location=new Point(26,380),Size=new Size(763,37)});
        new ToolTip().SetToolTip(preview,"Mostra il comando e i parametri che Avvia eseguira, senza avviare il modello.");
        new ToolTip().SetToolTip(layers,"DeepSeek V4 Flash: 43 livelli totali, 0-42 + output. Meta circa 21/22. Inserisci quanti assegnare al coordinatore.");
        maxOutput.ValueChanged+=delegate{Summary();};
    }
    void RefreshContextPresets(){
        if(changing)return;changing=true;
        int value=ContextValue;
        context.Maximum=Qwen?262144:1000000;
        if(value>context.Maximum)context.Value=context.Maximum;
        contextPreset.Items.Clear();
        foreach(int n in new[]{32768,65536,100000,131072,200000,262144})contextPreset.Items.Add(n.ToString());
        if(!Qwen)contextPreset.Items.Add("300000");
        contextPreset.Items.Add("Personalizzato");
        int index=contextPreset.Items.IndexOf(ContextValue.ToString());
        contextPreset.SelectedIndex=index<0?contextPreset.Items.Count-1:index;
        changing=false;
    }
    void UpdateAdvancedState(){
        if(role.SelectedIndex==1 && frontend.SelectedIndex!=0)frontend.SelectedIndex=0;
        frontend.Enabled=!busy && role.SelectedIndex!=1;
        network.Enabled=!busy&&!Solo;
        maxOutput.Enabled=context.Enabled=contextPreset.Enabled=preview.Enabled=!busy;
        customSplit.Enabled=!busy&&!Solo;layers.Enabled=!busy&&!Solo&&customSplit.Checked;
        if(!customSplit.Checked && role.SelectedIndex>=0 && gpu.SelectedIndex>=0)layers.Value=(role.SelectedIndex==0?gpu.SelectedIndex==0:gpu.SelectedIndex!=0)?24:19;
        configurePi.Enabled=!busy&&PiMode;
        contextNote.Text=ContextValue>300000?"Context oltre 300K: piu memoria KV/RAM/VRAM/SSD e prefill piu lungo.":"Context elevato aumenta memoria KV e prefill; 300K puo richiedere molta piu memoria di 100K.";
        contextNote.ForeColor=ContextValue>300000?Color.DarkRed:Color.DimGray;
    }
    async Task ConfigureEthernet(){Busy(true);try{using(var dialog=new EthernetSetup()){
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        await dialog.Apply();local.Text=dialog.LocalAddress;peer.Text=dialog.PeerAddress;Save();
        log.Text="Ethernet configurata: "+local.Text+" -> "+peer.Text+". Ripeti sull'altro PC. Ruoli DS4 indipendenti dal numero PC.";
    }}catch(Exception ex){Error(ex);}finally{Busy(false);}}
    static string Resource(string name){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){
        if(stream==null)throw new Exception("Risorsa incorporata mancante: "+name);
        using(var reader=new StreamReader(stream))return reader.ReadToEnd();
    }}
    static string PiCheck(){return Resource("AIutante.PiDiscovery.sh")+"\n";}
    static string PiHelper(string args){return "python3 -c \"$(printf %s "+B64(Resource("DS4.Pi.py"))+" | base64 -d)\" "+args+"\n";}
    static void ObserveFailure(Task task){task.ContinueWith(t=>{var observed=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);}
    Process OpenConsole(string script,string title){
        string ps="$Host.UI.RawUI.WindowTitle="+Ps(title)+"; $p=New-Object System.Diagnostics.Process; $p.StartInfo=New-Object System.Diagnostics.ProcessStartInfo; $p.StartInfo.FileName='wsl.exe'; $p.StartInfo.Arguments="+Ps(Args(script))+"; $p.StartInfo.UseShellExecute=$false; [void]$p.Start(); $p.WaitForExit(); Write-Host ('Processo terminato: '+$p.ExitCode); [void](Read-Host 'Invio per chiudere')";
        return LaunchPowerShell(ps,false);
    }
    async Task StartPi(){
        piStarting=true;
        log.Text=await CaptureWsl(PiCheck()+PiHelper("configure"+PiOptions)+"pi --list-models "+(Qwen?"qwen":"ds4")+"\n");
        string pidFile="/tmp/ds4-launcher-"+Guid.NewGuid().ToString("N")+".pid";
        string server="trap "+Sh("printf failed > "+Sh(pidFile+".finished"))+" EXIT\n"+Preflight(true)+MemoryEnvironment()+ModelCommand()+" &\nserver=$!\nprintf '%s' \"$server\" > "+Sh(pidFile)+"\nwait \"$server\"\n";
        modelConsole=OpenConsole(server,"AIutante LLM server - localhost:8000");modelDistribution=distro.Text;
        stop.Enabled=true;
        log.Text="Terminale server aperto. Apro Pi: attendera che il modello sia pronto.";
        string ocrPath="/home/ds4/aiutante-ocr/aiutante_ocr.py";
        string pi="set -e\necho "+Sh("Pi Agent: attendo il modello, fino a 15 minuti. Gli errori restano in questa finestra.")+"\n"+PiHelper("wait --pid-file "+Sh(pidFile)+" --timeout 900"+(Qwen?" --model qwen":""))+Resolve()+PiCheck()+"exec pi --offline --model "+PiModel+" --append-system-prompt "+Sh("OCR locale disponibile tramite /home/ds4/aiutante-ocr/.venv/bin/python "+ocrPath+" INPUT --output CARTELLA. Prima usa --check e verifica la VRAM libera con nvidia-smi. Se dipendenze/pesi o memoria mancano, segnala il limite; non fermare il modello o installare pacchetti senza richiesta. Salva i risultati OCR su disco e leggi solo il testo necessario.")+"\n";
        piConsole=OpenConsole(pi,"AIutante - Pi Agent");
        log.Text="Aperti server LLM e Pi Agent. Pi mostra l'attesa e parte appena il modello e pronto. Se fallisce, leggi l'errore nel terminale Pi.";

    }
    XElement AdvancedSettings(){return new XElement("advanced",new XElement("defaultsVersion",2),new XElement("model",Qwen?"qwen":"ds4"),new XElement("qwenFolder",qwenFolder.Text),new XElement("gpuLayers",(int)gpuLayers.Value),new XElement("frontend",PiMode?"pi":"ds4"),new XElement("network",network.SelectedIndex==1?"ethernet":"wifi"),new XElement("context",ContextValue),new XElement("maxOutput",OutputValue),new XElement("customSplit",customSplit.Checked),new XElement("layers",(int)layers.Value));}
    void ReadAdvanced(XElement root){var x=root.Element("advanced");if(x==null)return;model.SelectedIndex=(string)x.Element("model")=="qwen"?1:0;qwenFolder.Text=(string)x.Element("qwenFolder")??"/home/ds4/aiutante-qwen";gpuLayers.Value=Math.Max(-1,Math.Min(65,(int?)x.Element("gpuLayers")??-1));
        frontend.SelectedIndex=(string)x.Element("frontend")=="pi"?1:0;
        network.SelectedIndex=(string)x.Element("network")=="ethernet"?1:0;
        maxOutput.Value=Math.Max(1,Math.Min(1000000,(int?)x.Element("maxOutput")??32768));
        int n=(int?)x.Element("context")??100000;
        context.Maximum=Qwen?262144:1000000;context.Value=Math.Max(2048,Math.Min((int)context.Maximum,n));RefreshContextPresets();
        customSplit.Checked=(bool?)x.Element("customSplit")??false;
        layers.Value=Math.Max(1,Math.Min(42,(int?)x.Element("layers")??24));
    }
    static void Expect(bool value,string message){if(!value)throw new Exception(message);}
    static int OfflineTests(){
        string output=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-offline-tests.txt");
        try{
            using(var f=new DS4Launcher()){
                f.skipShutdown=true;f.model.SelectedIndex=0;f.customSplit.Checked=false;
                f.local.Text="192.168.1.12";f.peer.Text="192.168.1.10";
                f.distro.Text="Ubuntu-24.04";f.user.Text="ds4";f.folder.Text="/home/ds4/ds4";
                var commands=new System.Text.StringBuilder();int cases=0;
                foreach(int ctx in new[]{100000,300000,131072})foreach(int net in new[]{0,1})foreach(int r in new[]{0,1,2})foreach(int front in new[]{0,1}){
                    f.context.Value=ctx;f.network.SelectedIndex=net;f.role.SelectedIndex=r;f.frontend.SelectedIndex=front;f.gpu.SelectedIndex=0;
                    f.ValidateFields();string cmd=f.ModelCommand();
                    Expect(cmd.Contains("--ctx "+ctx),"Context mismatch");
                    Expect(cmd.Contains("--cuda --ssd-streaming")&&!cmd.Contains("--gpu-vram")&&!cmd.Contains("--gpu-devices"),"Streaming contract");
                    Expect((r!=2)==cmd.Contains("--role"),"Local/distributed role");
                    Expect((r!=1&&front==1)==cmd.StartsWith("./ds4-server "),"Pi engine selection");
                    if(r==1)Expect(!f.PiMode&&f.frontend.SelectedIndex==0&&!f.frontend.Enabled,"Worker Pi exclusion");
                    if(f.PiMode)Expect(cmd.Contains("--host 127.0.0.1 --port 8000")&&cmd.Contains("--kv-disk-space-mb 8192"),"HTTP loopback");
                    if(r==2)Expect(!f.Preflight(true).Contains("wslinfo")&&!f.network.Enabled,"Standalone network isolation");
                    commands.AppendLine(cmd);cases++;
                }
                f.role.SelectedIndex=0;f.frontend.SelectedIndex=1;f.network.SelectedIndex=1;f.context.Value=131072;f.customSplit.Checked=true;f.layers.Value=20;
                f.stopUbuntu.Checked=false;f.keepRam.Checked=false;
                string xml=f.Settings().ToString();
                using(var other=new DS4Launcher()){
                    other.skipShutdown=true;other.ApplySettings(XDocument.Parse(xml).Root);
                    Expect(other.PiMode&&other.network.SelectedIndex==1&&other.ContextValue==131072&&other.Count()==20&&!other.stopUbuntu.Checked&&!other.keepRam.Checked,"Settings roundtrip");
                    other.context.Value=100000;other.frontend.SelectedIndex=0;other.network.SelectedIndex=0;
                    other.ApplySettings(XElement.Parse("<settings><role>2</role><gpu>0</gpu></settings>"));
                    Expect(other.Solo&&other.ContextValue==100000&&!other.PiMode,"Legacy settings");
                }
                f.keepRam.Checked=true;Expect(f.MemoryEnvironment().Contains("KEEP_MODEL_PAGES=1"),"RAM on");
                f.keepRam.Checked=false;Expect(f.MemoryEnvironment().Contains("unset DS4_CUDA_KEEP_MODEL_PAGES"),"RAM off");
                Expect(f.Icon!=null&&Resource("DS4.Ethernet.ps1").Contains("192.168.250.")&&Resource("DS4.Pi.py").Contains("wait_ready"),"Embedded assets");
                f.model.SelectedIndex=1;f.context.Value=100000;f.maxOutput.Value=32000;
                foreach(int r in new[]{0,1,2})foreach(int front in new[]{0,1}){
                    f.role.SelectedIndex=r;f.frontend.SelectedIndex=front;f.ValidateFields();string cmd=f.ModelCommand();
                    Expect(!cmd.Contains("ssd-streaming"),"Qwen no SSD streaming");
                    if(r==1)Expect(cmd.Contains("ggml-rpc-server")&&!f.Resolve().Contains("test -s model.gguf"),"Qwen worker has no GGUF");
                    else {Expect(cmd.Contains("--predict 32000")&&cmd.Contains("--load-mode mlock")&&cmd.Contains("--lazy-mode off"),"Qwen output and resident weights");Expect(cmd.Contains("--rpc")== (r==0),"Qwen RPC role");}
                    cases++;
                }
                foreach(int ctx in new[]{131072,200000,262144}){f.context.Value=ctx;f.ValidateFields();Expect(f.ModelCommand().Contains("--ctx-size "+ctx),"Qwen context above 100K");}
                Expect(!f.contextPreset.Items.Contains("300000"),"Qwen excludes invalid 300K preset");
                f.context.Value=100000;
                f.role.SelectedIndex=2;f.frontend.SelectedIndex=0;f.gpuLayers.Value=20;
                Expect(f.ModelCommand().Contains("--gpu-layers 20"),"Qwen GPU override");
                using(var other=new DS4Launcher()){other.skipShutdown=true;other.ApplySettings(f.Settings().Root);Expect(other.Qwen&&other.OutputValue==32000&&other.gpuLayers.Value==20,"Qwen settings roundtrip");}
                f.model.SelectedIndex=0;
                Expect(f.ModelCommand().Contains("-n 32000"),"DS4 output configurable");
                f.role.SelectedIndex=0;f.frontend.SelectedIndex=1;f.context.Value=300000;f.customSplit.Checked=false;
                f.Show();Application.DoEvents();using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(0,0,f.Width,f.Height));bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-preview.png"));}f.Close();
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-command-matrix.txt"),commands.ToString());
                File.WriteAllText(output,"PASS: "+cases+" command combinations, contexts, worker exclusion, loopback HTTP, settings, legacy defaults, RAM, embedded icon/scripts. No network or WSL mutations.");
            }
            return 0;
        }catch(Exception ex){File.WriteAllText(output,"FAIL: "+ex);return 1;}
    }
}

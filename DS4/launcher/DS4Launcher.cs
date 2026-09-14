using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

partial class DS4Launcher : Form {
    TextBox local = new TextBox(), peer = new TextBox(), distro = new TextBox(), user = new TextBox(), folder = new TextBox();
    ComboBox role = new ComboBox(), gpu = new ComboBox();
    CheckBox keepRam = new CheckBox();
    CheckBox stopUbuntu = new CheckBox();
    Process modelConsole;
    bool closingReady, closingCheck, skipShutdown;
    bool busy;
    bool Solo { get { return role.SelectedIndex==2; } }
    Label summary = new Label();
    TextBox log = new TextBox();
    Button check = new Button(), start = new Button(), firewall = new Button(), stop = new Button();
    string modelDistribution;
    static string Store = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AIutante");
    string Config { get { return Path.Combine(Store, "settings.xml"); } }
    static string Sh(string s) { return "'" + s.Replace("'", "'\"'\"'") + "'"; }
    static string Ps(string s) { return "'" + s.Replace("'", "''") + "'"; }
    static string Win(string s) { return "\"" + Regex.Replace(s, "(\\\\*)\"", "$1$1\\\"").TrimEnd('\0') + new string('\\', TrailingSlashes(s)) + "\""; }
    static int TrailingSlashes(string s) { int n=0; for(int i=s.Length-1;i>=0 && s[i]=='\\';i--) n++; return n; }
    static string B64(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Replace("\r\n", "\n"))); }
    static string IPv4(string value) {
        IPAddress a;
        if (!Regex.IsMatch(value, @"^\d{1,3}(\.\d{1,3}){3}$") || !IPAddress.TryParse(value, out a) || a.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(a) || value=="0.0.0.0")
            throw new Exception("Inserisci un IPv4 LAN valido, per esempio 192.168.1.12.");
        return a.ToString();
    }
    public DS4Launcher() {
        Text="AIutante - Chat e Pi Agent"; ClientSize=new Size(820,957); MinimumSize=new Size(836,700); AutoScroll=true;
        Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font=new Font("Segoe UI",10); StartPosition=FormStartPosition.CenterScreen; BackColor=Color.FromArgb(245,247,250);
        var title=new Label {Text="AIutante - RAM, GPU e SSD", Font=new Font("Segoe UI",19,FontStyle.Bold),AutoSize=true,Location=new Point(24,16)}; Controls.Add(title);
        Controls.Add(new Label {Text="Scegli la chat su questo PC oppure distribuisci il modello su due PC.",AutoSize=true,Location=new Point(26,58)});
        role.DropDownStyle=gpu.DropDownStyle=ComboBoxStyle.DropDownList;
        role.Items.AddRange(new object[]{"Coordinatore - chat e calcolo","Worker - solo calcolo","Solo questo PC - chat locale"});
        gpu.Items.AddRange(new object[]{"RTX 5070 Ti - 16 GB","RTX 5070 - 12 GB"});
        Row("Ruolo di questo PC",role,98);
        InitModelControls();
        InitAdvancedControls();
        Row("GPU di questo PC (split automatico)",gpu,224);
        Row("IP di questo PC",local,384); Row("IP dell'altro PC",peer,426);
        Row("Distribuzione WSL",distro,468); Row("Utente Ubuntu (vuoto = predefinito)",user,510); Row("Cartella Linux del motore",folder,552);
        distro.Text="Ubuntu-24.04";
        local.Text=LocalIP(); peer.Text=local.Text=="192.168.1.10"?"192.168.1.12":"192.168.1.10";
        role.SelectedIndex=2; gpu.SelectedIndex=local.Text=="192.168.1.10"?1:0;
        keepRam.Text="Conserva in RAM i pesi letti (cache automatica entro i limiti WSL)";
        keepRam.Checked=true; keepRam.SetBounds(26,594,770,30); Controls.Add(keepRam);
        stopUbuntu.Text="Chiudi Ubuntu all'uscita, se terminale DS4 e servizi sono chiusi";
        stopUbuntu.Checked=true; stopUbuntu.SetBounds(26,626,770,30); Controls.Add(stopUbuntu);
        LoadConfig();
        summary.SetBounds(26,665,770,55); Controls.Add(summary);
        check.Text="Verifica"; check.SetBounds(26,729,105,38);
        firewall.Text="Configura rete"; firewall.SetBounds(139,729,145,38);
        start.Text="Avvia"; start.SetBounds(571,729,105,38);
        stop.Text="Stop"; stop.SetBounds(684,729,105,38);
        Controls.AddRange(new Control[]{check,firewall,start,stop});
        new ToolTip().SetToolTip(stop,"Arresta Ubuntu selezionato e tutti i suoi processi, inclusi modello e trasferimenti. L'altro PC resta acceso.");
        log.SetBounds(26,781,763,75); log.Multiline=true; log.ReadOnly=true; log.ScrollBars=ScrollBars.Vertical; Controls.Add(log);
        role.SelectedIndexChanged+=delegate{Summary();}; gpu.SelectedIndexChanged+=delegate{Summary();}; Summary();
        check.Click+=async delegate { await Check(); };
        start.Click+=async delegate { await StartModel(); };
        stop.Click+=async delegate { await StopModel(); };
        firewall.Click+=async delegate { if(network.SelectedIndex==1){await ConfigureEthernet();return;} try { ConfigureNetwork(); } catch(Exception e){Error(e);} };
        log.Text="RAM: cache recuperabile da Linux; cresce con le letture. VRAM: pesi e buffer gestiti da CUDA.\r\nSSD per i pesi mancanti. La cache RAM non e una cache persistente degli esperti in VRAM.";
        InitOutputControls();PlaceModelControls();
        FormClosing+=OnLauncherClosing;
    }
    static string TerminateArgs(string target){
        if(!Regex.IsMatch(target,@"^[A-Za-z0-9_.-]+$"))throw new Exception("Nome distribuzione non valido.");
        return "--terminate "+target;
    }
    static async Task RunWslControl(string args){await Task.Run(()=>{
        using(var p=Process.Start(new ProcessStartInfo("wsl.exe",args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true})){
            var error=p.StandardError.ReadToEndAsync();
            if(!p.WaitForExit(30000)){p.Kill();throw new Exception("Arresto Ubuntu non confermato entro 30 secondi.");}
            if(p.ExitCode!=0)throw new Exception(error.Result.Replace("\0",""));
        }
    });}
    async Task StopModel(){stopping=true;Busy(true);try{
        string target=modelDistribution??distro.Text;
        if(MessageBox.Show(this,"Stop arresta "+target+", inclusi modello, Pi e qualsiasi trasferimento in corso. Continuare?","Stop DS4 e Ubuntu",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning)!=DialogResult.OK)return;
        stopRequested=true;
        log.Text="Arresto di "+target+": modello, connessione e processi Ubuntu...";
        await RunWslControl(TerminateArgs(target));
        if(modelConsole!=null && !modelConsole.HasExited){modelConsole.Kill();modelConsole.WaitForExit(5000);}
        if(piConsole!=null && !piConsole.HasExited)piConsole.Kill();
        modelConsole=null;modelDistribution=null;
        bool released=await ShutdownIdleWsl();
        log.Text="Ubuntu "+target+" arrestato. Modello e connessione locale chiusi.\r\n"+(released?"Macchina virtuale WSL arrestata.":"Altre distribuzioni attive: VmmemWSL resta necessario.")+" L'altro PC resta acceso.";
    }catch(Exception ex){Error(ex);}finally{stopping=false;Busy(false);}}
    // Read-only guard: never terminate a model, transfer, diagnostic listener,
    // build or another interactive Ubuntu terminal just to reclaim memory.
    static string ShutdownGuard(){return @"set -e
if pgrep -x 'llama-cli|llama-server|ggml-rpc-server|ds4|ds4-server|ds4-agent|ds4-bench|ds4-eval|pi|node|make|nvcc|cc1plus|rsync|wget|curl' >/dev/null; then echo 'KEEP: modello, Pi, compilazione o trasferimento attivo'; exit 0; fi
if ps -eo tty= | grep -E '^ *(pts/|tty)' >/dev/null; then echo 'KEEP: terminale Ubuntu ancora aperto'; exit 0; fi
if ss -ltnH | awk '{print $4}' | grep -Ev ':53$' >/dev/null; then echo 'KEEP: servizio in ascolto (possibile trasferimento modello)'; exit 0; fi
echo SAFE_TO_TERMINATE
";}
    static async Task<bool> IsRunning(string target){return await Task.Run(()=>{
        using(var p=Process.Start(new ProcessStartInfo("wsl.exe","--list --running --quiet"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true})){
            var output=p.StandardOutput.ReadToEndAsync();
            if(!p.WaitForExit(15000)){p.Kill();throw new Exception("Impossibile verificare lo stato di Ubuntu.");}
            if(p.ExitCode!=0)throw new Exception("Impossibile elencare le distribuzioni attive.");
            foreach(string line in output.Result.Replace("\0","").Split('\n'))if(line.Trim().Length>0 && (target==null || line.Trim()==target))return true;
            return false;
        }
    });}
    static async Task<bool> ShutdownIdleWsl(){
        if(await IsRunning(null))return false;
        await RunWslControl("--shutdown");
        for(int i=0;i<50;i++){
            if(Process.GetProcessesByName("vmmemWSL").Length==0)return true;
            await Task.Delay(200);
        }
        throw new Exception("Arresto WSL richiesto, ma VmmemWSL risulta ancora presente. Un altro programma potrebbe riavviare WSL.");
    }
    async void OnLauncherClosing(object sender,FormClosingEventArgs e){
        if(closingReady || skipShutdown)return;
        if(busy || closingCheck){e.Cancel=true;log.AppendText("\r\nAttendi il completamento dell'operazione.");return;}
        if(!stopUbuntu.Checked)return;
        e.Cancel=true;closingCheck=true;Busy(true);
        try{
            if(modelConsole!=null && !modelConsole.HasExited)throw new Exception("Ubuntu resta acceso: il terminale del modello e ancora aperto.");
            if(piConsole!=null && !piConsole.HasExited)throw new Exception("Ubuntu resta acceso: il terminale Pi e ancora aperto.");
            if(modelDistribution!=null && modelDistribution!=distro.Text)throw new Exception("Distribuzione cambiata dopo l'avvio: Ubuntu resta acceso. Usa Stop per arrestare la sessione avviata.");
            if(!Regex.IsMatch(distro.Text,@"^[A-Za-z0-9_.-]+$") || (user.Text.Length>0 && !Regex.IsMatch(user.Text,@"^[a-z_][a-z0-9_-]*$")))throw new Exception("Nome distribuzione o utente non valido: Ubuntu non arrestato.");
            Save();log.Text="Controllo prima dell'arresto di Ubuntu...";
            if(!await IsRunning(distro.Text)){await ShutdownIdleWsl();return;}
            string guard=await CaptureWsl(ShutdownGuard());
            if(guard.Trim()!="SAFE_TO_TERMINATE")throw new Exception("Ubuntu resta acceso. "+guard.Trim());
            string target=distro.Text;
            await RunWslControl(TerminateArgs(target));
            await ShutdownIdleWsl();
        }catch(Exception ex){MessageBox.Show(this,ex.Message,"Chiusura DS4",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        finally{closingReady=true;closingCheck=false;Close();}
    }
    void Row(string name,Control c,int y){Controls.Add(new Label{Text=name,Location=new Point(26,y+5),Size=new Size(285,28)});c.SetBounds(319,y,470,31);Controls.Add(c);}
    static string LocalIP(){foreach(var n in NetworkInterface.GetAllNetworkInterfaces()) if(n.OperationalStatus==OperationalStatus.Up && (n.NetworkInterfaceType==NetworkInterfaceType.Wireless80211 || n.NetworkInterfaceType==NetworkInterfaceType.Ethernet)) foreach(var a in n.GetIPProperties().UnicastAddresses) if(a.Address.AddressFamily==AddressFamily.InterNetwork && a.Address.ToString().StartsWith("192.168.")) return a.Address.ToString(); return "";}
    int Count(){return customSplit.Checked?(int)layers.Value:(role.SelectedIndex==0 ? gpu.SelectedIndex==0 : gpu.SelectedIndex!=0)?24:19;}
    void Summary(){UpdateAdvancedState();int n=Count();summary.Text=(Solo?"Tutto il modello su questo PC.":role.SelectedIndex==0?"Coordinatore: livelli 0-"+(n-1)+". Worker sull'altro PC.":"Worker: livelli "+n+"-42 e output. Prompt sul coordinatore.")+" Totale: 43 livelli (0-42) + output; split "+n+"/"+(43-n)+". Context: "+ContextValue+"; output max: "+OutputValue+".\r\n"+(PiMode?"Pi Agent: API locale 127.0.0.1:8000; tool su questo PC.":"Chat DS4: avanzamento e token/s nel titolo della console.");local.Enabled=peer.Enabled=firewall.Enabled=!busy&&!Solo;gpu.Enabled=!busy&&!Solo;ModelState();}
    void ValidateFields(){if(Qwen&&(ContextValue>262144||!qwenFolder.Text.StartsWith("/")||qwenFolder.Text.Contains("\n")||qwenFolder.Text.Contains("\r")))throw new Exception("Qwen: context massimo 262144 e cartella Linux assoluta necessaria.");if(!Solo){local.Text=IPv4(local.Text.Trim());peer.Text=IPv4(peer.Text.Trim());if(local.Text==peer.Text)throw new Exception("I due PC devono avere indirizzi diversi.");}if(!Regex.IsMatch(distro.Text,@"^[A-Za-z0-9_.-]+$") || (user.Text.Length>0 && !Regex.IsMatch(user.Text,@"^[a-z_][a-z0-9_-]*$")))throw new Exception("Nome distribuzione o utente Ubuntu non valido.");if(folder.Text.Length>0 && (!folder.Text.StartsWith("/") || folder.Text.Contains("\n") || folder.Text.Contains("\r")))throw new Exception("Usa un percorso Linux assoluto, ad esempio /home/nome/DS4_InferenzaMista.");}
    string Args(string script){return "-d "+distro.Text+(user.Text.Length>0?" -u "+user.Text:"")+" --exec bash -lc "+Win("eval \"$(printf %s "+B64(script)+" | base64 -d)\"");}
    string Resolve(){if(Qwen)return QwenResolve();return "set -e\nengine="+Sh(PiMode?"ds4-server":"ds4")+"\nroot="+Sh(folder.Text)+"\n"+@"if [ -z ""$root"" ]; then
  for candidate in ""$HOME/AIutante/DS4"" ""$HOME/DS4_InferenzaMista/DS4"" ""$HOME/DS4_InferenzaMista"" ""$HOME/ds4"" /home/ds4/ds4; do
    if [ -x ""$candidate/$engine"" ]; then root=$candidate; break; fi
  done
fi
[ -n ""$root"" ] && [ -x ""$root/$engine"" ] || { echo 'ERRORE: binario ds4/ds4-server non trovato. Compila il motore e controlla cartella/utente.'; exit 1; }
cd ""$root""
[ -s ds4flash.gguf ] || { echo 'ERRORE: modello ds4flash.gguf mancante o trasferimento non completato.'; exit 1; }
";}
    string Preflight(bool launching){if(Qwen)return QwenPreflight(launching);return Resolve()+"echo 'Cartella:' \"$root\"\n"+(Solo?"":@"mode=$(wslinfo --networking-mode)
echo ""Rete WSL: $mode""
[ ""$mode"" = mirrored ] || { echo 'ERRORE: configura mirrored in .wslconfig e riavvia WSL quando non ci sono lavori attivi.'; exit 1; }
"+"ip -4 -o addr show | grep -F ' "+local.Text+"/' >/dev/null || { echo 'ERRORE: IP locale non presente in Ubuntu.'; exit 1; }\n")+
"nvidia-smi --query-gpu=name,memory.total,memory.used --format=csv\nfree -h\n"+(PiMode?PiCheck():"")+
(launching ? "if pgrep -x 'ds4|ds4-server|llama-cli|llama-server|ggml-rpc-server' >/dev/null; then echo 'ERRORE: DS4 gia attivo. Chiudi prima la sessione precedente.'; exit 1; fi\n"+(Solo?"":"if [ -n \"$(ss -ltnH 'sport = :"+(role.SelectedIndex==0?"9911":"9912")+"')\" ]; then echo 'ERRORE: porta occupata, forse da un listener diagnostico. Fermalo prima di avviare.'; exit 1; fi\n")+(PiMode?"if [ -n \"$(ss -ltnH 'sport = :8000')\" ]; then echo 'ERRORE: porta HTTP 8000 gia occupata'; exit 1; fi\n":"") : "")+"echo VERIFICA_OK\n";}
    async Task<string> CaptureWsl(string script,int timeout=45000){string args=Args(script);return await Task.Run(()=>{using(var p=new Process()){p.StartInfo=new ProcessStartInfo("wsl.exe",args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};p.Start();var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();if(!p.WaitForExit(timeout)){p.Kill();throw new Exception("Verifica scaduta: controlla avvio e configurazione di Ubuntu.");}Task.WaitAll(output,error);string text=output.Result+error.Result;if(p.ExitCode!=0)throw new Exception(text);return text;}});}
    void Busy(bool b){busy=b||piStarting||stopping;check.Enabled=start.Enabled=stop.Enabled=distro.Enabled=user.Enabled=folder.Enabled=role.Enabled=keepRam.Enabled=stopUbuntu.Enabled=!busy;Summary();if(piStarting&&!stopping)stop.Enabled=true;}
    void Error(Exception e){log.Text=e.Message;MessageBox.Show(this,e.Message,"DS4",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    async Task Check(){Busy(true);try{ValidateFields();Save();log.Text="Verifica in corso...";log.Text=await CaptureWsl(Preflight(false));}catch(Exception e){Error(e);}finally{Busy(false);}}
    static string Command(bool coordinator,int count,string ip,int context=100000,int output=16384){return "./ds4 -m ds4flash.gguf --cuda --ssd-streaming --ctx "+context+" --prefill-chunk 64 --nothink "+(coordinator?"--role coordinator --layers 0:"+(count-1)+" --listen "+ip+" 9911 -n "+output:"--role worker --layers "+count+":output --listen 0.0.0.0 9912 --coordinator "+ip+" 9911");}
    string ModelCommand(){if(Qwen)return QwenCommand();string command=Solo?"./ds4 -m ds4flash.gguf --cuda --ssd-streaming --ctx "+ContextValue+" --prefill-chunk 64 --nothink -n "+OutputValue:Command(role.SelectedIndex==0,Count(),role.SelectedIndex==0?local.Text:peer.Text,ContextValue,OutputValue);return PiMode?command.Replace("./ds4 ","./ds4-server ").Replace(" --nothink","").Replace(" -n "+OutputValue,"")+" --host 127.0.0.1 --port 8000 --kv-disk-dir \"$HOME/.cache/ds4-kv\" --kv-disk-space-mb 8192":command;}
    string MemoryEnvironment(){if(Qwen)return "";return "export DS4_CUDA_NO_DIRECT_IO=1\n"+(keepRam.Checked?"export DS4_CUDA_KEEP_MODEL_PAGES=1\n":"unset DS4_CUDA_KEEP_MODEL_PAGES\n");}
    async Task StartModel(){Busy(true);try{ValidateFields();Save();log.Text="Controllo prima dell'avvio...";log.Text=await CaptureWsl(Preflight(true));if(PiMode){await StartPi();return;}string script=Preflight(true)+MemoryEnvironment()+"echo "+Sh(Solo?"CHAT LOCALE: invia i prompt qui.":role.SelectedIndex==0?"COORDINATORE: invia i prompt qui. Attendi il worker.":"WORKER: solo calcolo, prompt sull'altro PC.")+"\nexec "+ModelCommand()+"\n";
        // A separate console owns stdin; the worker is never exposed as a chat.
        string ps="$Host.UI.RawUI.WindowTitle = "+Ps("AIutante - "+(Solo?"Chat locale":role.SelectedIndex==0?"Coordinatore":"Worker"))+"; $p = New-Object System.Diagnostics.Process; $p.StartInfo = New-Object System.Diagnostics.ProcessStartInfo; $p.StartInfo.FileName = 'wsl.exe'; $p.StartInfo.Arguments = "+Ps(Args(script))+"; $p.StartInfo.UseShellExecute = $false; [void]$p.Start(); $p.WaitForExit(); Write-Host ('DS4 terminato, codice ' + $p.ExitCode); [void](Read-Host 'Invio per chiudere')";
        modelConsole=LaunchPowerShell(ps,false);modelDistribution=distro.Text;log.AppendText("\r\nConsole aperta. Stop arresta modello e Ubuntu, inclusi altri processi nella distribuzione.");
    }catch(Exception e){Error(e);}finally{piStarting=false;Busy(false);}}
    static Process LaunchPowerShell(string script,bool admin){var p=new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes(script))){UseShellExecute=true};if(admin)p.Verb="runas";return Process.Start(p);}
    void ConfigureNetwork(){ValidateFields();Save();if(MessageBox.Show(this,"Consentire a "+peer.Text+" di collegarsi a questo PC sulle porte TCP 9911 e 9912?\n\nLe regole saranno limitate ai due IP e permetteranno di invertire i ruoli. WSL e il trasferimento del modello non verranno riavviati.","Firewall Windows e WSL",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
        string script="$ErrorActionPreference='Stop'; try { "+
        "$vm='{40E0AC32-46A5-438A-A0B2-2B479E8F2E90}'; foreach($port in @(9911,9912)) { $name='DS4Launcher-'+$port; "+
        "Get-NetFirewallRule -Name $name -ErrorAction SilentlyContinue | Remove-NetFirewallRule; "+
        "New-NetFirewallRule -Name $name -DisplayName $name -Direction Inbound -Action Allow -Protocol TCP -LocalPort $port -LocalAddress "+Ps(local.Text)+" -RemoteAddress "+Ps(peer.Text)+" -Profile Any | Out-Null; "+
        // Mirrored networking exposes Windows rules in Hyper-V too. Use a distinct
        // name for the explicit WSL rule; never remove its inherited Windows copy.
        "$hvName=$name+'-WSL'; $hvArgs=@{Name=$hvName; Direction='Inbound'; Action='Allow'; VMCreatorId=$vm; Protocol='TCP'; LocalPorts=[string]$port; LocalAddresses="+Ps(local.Text)+"; RemoteAddresses="+Ps(peer.Text)+"}; "+
        "if (Get-NetFirewallHyperVRule -Name $hvName -ErrorAction SilentlyContinue) { Set-NetFirewallHyperVRule @hvArgs -Enabled True } else { New-NetFirewallHyperVRule @hvArgs -DisplayName $hvName | Out-Null }; }; Write-Host 'Regole applicate. Usa Verifica nel launcher.' } catch {Write-Host ($_ | Out-String) -ForegroundColor Red; Write-Host $_.ScriptStackTrace}; [void](Read-Host 'Invio per chiudere')";
        LaunchPowerShell(script,true);log.Text="Richiesta configurazione firewall aperta. Controlla l'esito nella finestra amministrativa. Nessuna modifica alla porta 9913.";}
    XDocument Settings(){return new XDocument(new XElement("settings",new XElement("local",local.Text),new XElement("peer",peer.Text),new XElement("distro",distro.Text),new XElement("user",user.Text),new XElement("folder",folder.Text),new XElement("role",role.SelectedIndex),new XElement("gpu",gpu.SelectedIndex),new XElement("keepRam",keepRam.Checked),new XElement("stopUbuntu",stopUbuntu.Checked),AdvancedSettings()));}
    void Save(){Directory.CreateDirectory(Store);Settings().Save(Config);}
    void ApplySettings(XElement x){local.Text=(string)x.Element("local")??local.Text;peer.Text=(string)x.Element("peer")??peer.Text;distro.Text=(string)x.Element("distro")??distro.Text;user.Text=(string)x.Element("user")??"";folder.Text=(string)x.Element("folder")??"";role.SelectedIndex=Math.Max(0,Math.Min(2,(int?)x.Element("role")??2));gpu.SelectedIndex=Math.Max(0,Math.Min(1,(int?)x.Element("gpu")??0));keepRam.Checked=(bool?)x.Element("keepRam")??true;stopUbuntu.Checked=(bool?)x.Element("stopUbuntu")??true;ReadAdvanced(x);}
    void LoadConfig(){string source=File.Exists(Config)?Config:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DS4Launcher","settings.xml");if(!File.Exists(source))return;try{ApplySettings(XDocument.Load(source).Root);}catch{}}
    [STAThread] static int Main(string[] args){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length>0 && args[0]=="--self-test-offline")return OfflineTests();
        if(args.Length>0 && args[0]=="--self-test"){
            string output=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-self-test.txt");
            try{
                if(!Command(true,24,"192.168.1.12").Contains("--layers 0:23") || !Command(false,24,"192.168.1.12").Contains("--layers 24:output") || !Command(true,19,"192.168.1.10").Contains("--layers 0:18") || !Command(false,19,"192.168.1.10").Contains("--layers 19:output"))throw new Exception("Layer mapping");
                bool rejected=false;try{IPv4("192.168.1.10;echo x");}catch{rejected=true;}if(!rejected)throw new Exception("IP validation");
                if(TerminateArgs("Ubuntu-24.04")!="--terminate Ubuntu-24.04")throw new Exception("Stop scope");
                rejected=false;try{TerminateArgs("Ubuntu;whoami");}catch{rejected=true;}if(!rejected)throw new Exception("Stop validation");
                using(var f=new DS4Launcher()){
                    f.skipShutdown=true;
                    f.frontend.SelectedIndex=0;f.network.SelectedIndex=0;f.customSplit.Checked=false;
                    f.user.Text="ds4";f.local.Text="192.168.1.12";f.peer.Text="192.168.1.10";f.folder.Text="/home/ds4/ds4";
                    f.role.SelectedIndex=2; f.local.Text=""; f.peer.Text="";
                    f.ValidateFields();
                    if(f.ModelCommand().Contains("--role") || f.Preflight(true).Contains("wslinfo") || f.Preflight(true).Contains("ss -ltn") || !f.Preflight(true).Contains("pgrep"))throw new Exception("Standalone isolation");
                    f.keepRam.Checked=true;if(!f.MemoryEnvironment().Contains("export DS4_CUDA_KEEP_MODEL_PAGES=1"))throw new Exception("RAM cache enabled");
                    f.keepRam.Checked=false;if(!f.MemoryEnvironment().Contains("unset DS4_CUDA_KEEP_MODEL_PAGES"))throw new Exception("RAM cache disabled");
                    f.keepRam.Checked=true;
                    foreach(int r in new[]{0,1}) foreach(int g in new[]{0,1}) {
                        f.role.SelectedIndex=r;f.gpu.SelectedIndex=g;
                        f.local.Text="192.168.1.12";f.peer.Text="192.168.1.10";
                        f.ValidateFields();
                        int count=(r==0?g==0:g!=0)?24:19;
                        if(f.Count()!=count || !f.Preflight(true).Contains("ss -ltn"))throw new Exception("Distributed role regression");
                    }
                    f.role.SelectedIndex=2;f.local.Text="";f.peer.Text="";
                    string expected="spaces ' quotes \" and $ shell & symbols";
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-test-args.txt"),f.Args("printf %s "+Sh(expected)));
                    using(var p=Process.Start(new ProcessStartInfo("wsl.exe",f.Args("printf %s "+Sh(expected))){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true})){
                        string actual=p.StandardOutput.ReadToEnd();p.WaitForExit();if(p.ExitCode!=0 || actual!=expected)throw new Exception("Windows/WSL quoting: "+actual);
                    }
                    using(var p=Process.Start(new ProcessStartInfo("wsl.exe",f.Args(f.Preflight(false))){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true})){
                        string actual=p.StandardOutput.ReadToEnd();p.WaitForExit();if(p.ExitCode!=0 || !actual.Contains("VERIFICA_OK"))throw new Exception("Preflight: "+actual);
                        File.WriteAllText(output,"PASS: standalone without IP/network, four distributed roles, RAM cache on/off, IP validation, Windows/WSL quoting, real WSL preflight\r\n"+actual);
                    }
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preflight-test.sh"),f.Preflight(true),new UTF8Encoding(false));
                    f.Show();Application.DoEvents();using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(0,0,f.Width,f.Height));bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-preview.png"));}f.Close();
                }
                return 0;
            }catch(Exception e){File.WriteAllText(output,"FAIL: "+e);return 1;}
        }
        Application.Run(new DS4Launcher());return 0;
    }
}

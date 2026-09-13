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

class DS4Launcher : Form {
    TextBox local = new TextBox(), peer = new TextBox(), distro = new TextBox(), user = new TextBox(), folder = new TextBox();
    ComboBox role = new ComboBox(), gpu = new ComboBox();
    Label summary = new Label();
    TextBox log = new TextBox();
    Button check = new Button(), start = new Button(), firewall = new Button();
    static string Store = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DS4Launcher");
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
        Text="DS4 - Due PC, una chat"; ClientSize=new Size(730,640); MinimumSize=new Size(746,679);
        Font=new Font("Segoe UI",10); StartPosition=FormStartPosition.CenterScreen; BackColor=Color.FromArgb(245,247,250);
        var title=new Label {Text="DeepSeek su due computer", Font=new Font("Segoe UI",19,FontStyle.Bold),AutoSize=true,Location=new Point(24,16)}; Controls.Add(title);
        Controls.Add(new Label {Text="Entrambe le GPU calcolano. Solo il coordinatore riceve i prompt.",AutoSize=true,Location=new Point(26,58)});
        role.DropDownStyle=gpu.DropDownStyle=ComboBoxStyle.DropDownList;
        role.Items.AddRange(new object[]{"Coordinatore - chat e calcolo","Worker - solo calcolo"});
        gpu.Items.AddRange(new object[]{"RTX 5070 Ti - 16 GB","RTX 5070 - 12 GB"});
        Row("Ruolo di questo PC",role,98); Row("GPU di questo PC",gpu,140);
        Row("IP di questo PC",local,182); Row("IP dell'altro PC",peer,224);
        Row("Distribuzione WSL",distro,266); Row("Utente Ubuntu (vuoto = predefinito)",user,308); Row("Cartella Linux DS4 (vuoto = cerca)",folder,350);
        distro.Text="Ubuntu-24.04";
        local.Text=LocalIP(); peer.Text=local.Text=="192.168.1.10"?"192.168.1.12":"192.168.1.10";
        role.SelectedIndex=local.Text=="192.168.1.10"?1:0; gpu.SelectedIndex=local.Text=="192.168.1.10"?1:0;
        LoadConfig();
        summary.SetBounds(26,393,680,45); Controls.Add(summary);
        check.Text="1. Verifica"; check.SetBounds(26,444,155,38);
        firewall.Text="2. Configura rete"; firewall.SetBounds(193,444,190,38);
        start.Text="3. Avvia"; start.SetBounds(395,444,150,38);
        Controls.AddRange(new Control[]{check,firewall,start});
        log.SetBounds(26,497,678,117); log.Multiline=true; log.ReadOnly=true; log.ScrollBars=ScrollBars.Vertical; log.Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top|AnchorStyles.Bottom; Controls.Add(log);
        role.SelectedIndexChanged+=delegate{Summary();}; gpu.SelectedIndexChanged+=delegate{Summary();}; Summary();
        check.Click+=async delegate { await Check(); };
        start.Click+=async delegate { await StartModel(); };
        firewall.Click+=delegate { try { ConfigureNetwork(); } catch(Exception e){Error(e);} };
        log.Text="Prima configurazione: scegli ruoli opposti sui due PC e la GPU corretta.\r\nWSL, CUDA, binario e modello locale sono necessari. Pipeline ancora da collaudare.";
    }
    void Row(string name,Control c,int y){Controls.Add(new Label{Text=name,Location=new Point(26,y+5),Size=new Size(285,28)});c.SetBounds(319,y,385,31);Controls.Add(c);}
    static string LocalIP(){foreach(var n in NetworkInterface.GetAllNetworkInterfaces()) if(n.OperationalStatus==OperationalStatus.Up && (n.NetworkInterfaceType==NetworkInterfaceType.Wireless80211 || n.NetworkInterfaceType==NetworkInterfaceType.Ethernet)) foreach(var a in n.GetIPProperties().UnicastAddresses) if(a.Address.AddressFamily==AddressFamily.InterNetwork && a.Address.ToString().StartsWith("192.168.")) return a.Address.ToString(); return "";}
    int Count(){return (role.SelectedIndex==0 ? gpu.SelectedIndex==0 : gpu.SelectedIndex!=0)?24:19;}
    void Summary(){int n=Count();summary.Text=role.SelectedIndex==0?"Qui scrivi i prompt. Livelli locali: 0-"+(n-1)+".\r\nSull'altro PC scegli Worker; attendi che si colleghi.":"Qui non si inviano prompt. Livelli locali: "+n+"-42 e output.\r\nSull'altro PC scegli Coordinatore.";}
    void ValidateFields(){local.Text=IPv4(local.Text.Trim());peer.Text=IPv4(peer.Text.Trim());if(local.Text==peer.Text)throw new Exception("I due PC devono avere indirizzi diversi.");if(!Regex.IsMatch(distro.Text,@"^[A-Za-z0-9_.-]+$") || (user.Text.Length>0 && !Regex.IsMatch(user.Text,@"^[a-z_][a-z0-9_-]*$")))throw new Exception("Nome distribuzione o utente Ubuntu non valido.");if(folder.Text.Length>0 && (!folder.Text.StartsWith("/") || folder.Text.Contains("\n") || folder.Text.Contains("\r")))throw new Exception("Usa un percorso Linux assoluto, ad esempio /home/nome/DS4_InferenzaMista.");}
    string Args(string script){return "-d "+distro.Text+(user.Text.Length>0?" -u "+user.Text:"")+" --exec bash -lc "+Win("eval \"$(printf %s "+B64(script)+" | base64 -d)\"");}
    string Resolve(){return "set -e\nroot="+Sh(folder.Text)+"\n"+@"if [ -z ""$root"" ]; then
  for candidate in ""$HOME/DS4_InferenzaMista"" ""$HOME/ds4"" /home/ds4/ds4; do
    if [ -x ""$candidate/ds4"" ]; then root=$candidate; break; fi
  done
fi
[ -n ""$root"" ] && [ -x ""$root/ds4"" ] || { echo 'ERRORE: binario ds4 non trovato. Indica cartella e utente Ubuntu corretti.'; exit 1; }
cd ""$root""
[ -s ds4flash.gguf ] || { echo 'ERRORE: modello ds4flash.gguf mancante o trasferimento non completato.'; exit 1; }
";}
    string Preflight(bool launching){return Resolve()+"echo 'Cartella:' \"$root\"\n"+@"mode=$(wslinfo --networking-mode)
echo ""Rete WSL: $mode""
[ ""$mode"" = mirrored ] || { echo 'ERRORE: configura mirrored in .wslconfig e riavvia WSL quando non ci sono lavori attivi.'; exit 1; }
"+"ip -4 -o addr show | grep -F ' "+local.Text+"/' >/dev/null || { echo 'ERRORE: IP locale non presente in Ubuntu.'; exit 1; }\n"+
"nvidia-smi --query-gpu=name,memory.total,memory.used --format=csv\n"+
(launching ? "if pgrep -x ds4 >/dev/null || pgrep -x ds4-server >/dev/null; then echo 'ERRORE: DS4 gia attivo. Chiudi prima la sessione precedente.'; exit 1; fi\nif [ -n \"$(ss -ltnH 'sport = :"+(role.SelectedIndex==0?"9911":"9912")+"')\" ]; then echo 'ERRORE: porta occupata, forse da un listener diagnostico. Fermalo prima di avviare.'; exit 1; fi\n" : "")+"echo VERIFICA_OK\n";}
    async Task<string> CaptureWsl(string script){string args=Args(script);return await Task.Run(()=>{using(var p=new Process()){p.StartInfo=new ProcessStartInfo("wsl.exe",args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};p.Start();var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();if(!p.WaitForExit(45000)){p.Kill();throw new Exception("Verifica scaduta: controlla avvio e configurazione di Ubuntu.");}Task.WaitAll(output,error);string text=output.Result+error.Result;if(p.ExitCode!=0)throw new Exception(text);return text;}});}
    void Busy(bool b){check.Enabled=start.Enabled=firewall.Enabled=!b;local.Enabled=peer.Enabled=distro.Enabled=user.Enabled=folder.Enabled=role.Enabled=gpu.Enabled=!b;}
    void Error(Exception e){log.Text=e.Message;MessageBox.Show(this,e.Message,"DS4",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    async Task Check(){Busy(true);try{ValidateFields();Save();log.Text="Verifica in corso...";log.Text=await CaptureWsl(Preflight(false));}catch(Exception e){Error(e);}finally{Busy(false);}}
    static string Command(bool coordinator,int count,string ip){return "./ds4 --cuda --ssd-streaming --ctx 2048 --prefill-chunk 64 --nothink "+(coordinator?"--role coordinator --layers 0:"+(count-1)+" --listen "+ip+" 9911 -n 256":"--role worker --layers "+count+":output --listen 0.0.0.0 9912 --coordinator "+ip+" 9911");}
    async Task StartModel(){Busy(true);try{ValidateFields();Save();log.Text="Controllo prima dell'avvio...";log.Text=await CaptureWsl(Preflight(true));string script=Preflight(true)+"export DS4_CUDA_NO_DIRECT_IO=1\necho "+Sh(role.SelectedIndex==0?"COORDINATORE: invia i prompt qui. Attendi il worker.":"WORKER: solo calcolo, prompt sull'altro PC.")+"\nexec "+Command(role.SelectedIndex==0,Count(),role.SelectedIndex==0?local.Text:peer.Text)+"\n";
        // A separate console owns stdin; the worker is never exposed as a chat.
        string ps="$Host.UI.RawUI.WindowTitle = "+Ps("DS4 - "+(role.SelectedIndex==0?"Coordinatore":"Worker"))+"; $p = New-Object System.Diagnostics.Process; $p.StartInfo = New-Object System.Diagnostics.ProcessStartInfo; $p.StartInfo.FileName = 'wsl.exe'; $p.StartInfo.Arguments = "+Ps(Args(script))+"; $p.StartInfo.UseShellExecute = $false; [void]$p.Start(); $p.WaitForExit(); Write-Host ('DS4 terminato, codice ' + $p.ExitCode); [void](Read-Host 'Invio per chiudere')";
        LaunchPowerShell(ps,false);log.AppendText("\r\nConsole aperta. Puoi chiudere questo launcher senza fermare DS4.");
    }catch(Exception e){Error(e);}finally{Busy(false);}}
    static void LaunchPowerShell(string script,bool admin){var p=new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes(script))){UseShellExecute=true};if(admin)p.Verb="runas";Process.Start(p);}
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
    void Save(){Directory.CreateDirectory(Store);new XDocument(new XElement("settings",new XElement("local",local.Text),new XElement("peer",peer.Text),new XElement("distro",distro.Text),new XElement("user",user.Text),new XElement("folder",folder.Text),new XElement("role",role.SelectedIndex),new XElement("gpu",gpu.SelectedIndex))).Save(Config);}
    void LoadConfig(){if(!File.Exists(Config))return;try{var x=XDocument.Load(Config).Root;local.Text=(string)x.Element("local");peer.Text=(string)x.Element("peer");distro.Text=(string)x.Element("distro");user.Text=(string)x.Element("user");folder.Text=(string)x.Element("folder");role.SelectedIndex=(int)x.Element("role");gpu.SelectedIndex=(int)x.Element("gpu");}catch{}}
    [STAThread] static int Main(string[] args){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length>0 && args[0]=="--self-test"){
            string output=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-self-test.txt");
            try{
                if(!Command(true,24,"192.168.1.12").Contains("--layers 0:23") || !Command(false,24,"192.168.1.12").Contains("--layers 24:output") || !Command(true,19,"192.168.1.10").Contains("--layers 0:18") || !Command(false,19,"192.168.1.10").Contains("--layers 19:output"))throw new Exception("Layer mapping");
                bool rejected=false;try{IPv4("192.168.1.10;echo x");}catch{rejected=true;}if(!rejected)throw new Exception("IP validation");
                using(var f=new DS4Launcher()){
                    f.user.Text="ds4";f.local.Text="192.168.1.12";f.peer.Text="192.168.1.10";f.folder.Text="/home/ds4/ds4";
                    string expected="spaces ' quotes \" and $ shell & symbols";
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-test-args.txt"),f.Args("printf %s "+Sh(expected)));
                    using(var p=Process.Start(new ProcessStartInfo("wsl.exe",f.Args("printf %s "+Sh(expected))){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true})){
                        string actual=p.StandardOutput.ReadToEnd();p.WaitForExit();if(p.ExitCode!=0 || actual!=expected)throw new Exception("Windows/WSL quoting: "+actual);
                    }
                    using(var p=Process.Start(new ProcessStartInfo("wsl.exe",f.Args(f.Preflight(false))){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true})){
                        string actual=p.StandardOutput.ReadToEnd();p.WaitForExit();if(p.ExitCode!=0 || !actual.Contains("VERIFICA_OK"))throw new Exception("Preflight: "+actual);
                        File.WriteAllText(output,"PASS: roles, IP validation, Windows/WSL quoting, real WSL preflight\r\n"+actual);
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

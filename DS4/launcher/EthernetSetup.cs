using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

// Windows creates an IP network; DS4 continues to use its existing TCP protocol.
sealed class EthernetSetup : Form {
    ComboBox adapters=new ComboBox(), pc=new ComboBox();
    Label status=new Label();
    public int PcNumber { get { return pc.SelectedIndex+1; } }
    public string LocalAddress { get { return "192.168.250."+PcNumber; } }
    public string PeerAddress { get { return "192.168.250."+(3-PcNumber); } }
    sealed class Adapter {
        public NetworkInterface Nic;
        public override string ToString(){return Nic.Name+" - "+Nic.Description;}
    }
    public EthernetSetup(){
        Text="Collegamento Ethernet diretto";ClientSize=new Size(650,320);
        FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
        StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
        Controls.Add(new Label{Text="Collega le porte Ethernet dei due PC con un cavo Cat 5e o Cat 6.\nScegli PC1 su un computer e PC2 sull'altro, indipendentemente dal ruolo DS4.",Location=new Point(18,16),Size=new Size(615,52)});
        adapters.DropDownStyle=pc.DropDownStyle=ComboBoxStyle.DropDownList;
        adapters.SetBounds(18,82,614,32);pc.SetBounds(18,125,614,32);
        foreach(var nic in NetworkInterface.GetAllNetworkInterfaces())
            if(nic.NetworkInterfaceType==NetworkInterfaceType.Ethernet && !nic.Description.Contains("Hyper-V"))adapters.Items.Add(new Adapter{Nic=nic});
        pc.Items.AddRange(new object[]{"PC1 - 192.168.250.1","PC2 - 192.168.250.2"});
        pc.SelectedIndex=0;if(adapters.Items.Count>0)adapters.SelectedIndex=0;
        status.SetBounds(18,174,614,76);
        var apply=new Button{Text="Configura (UAC)",Location=new Point(320,268),Size=new Size(160,34)};
        var cancel=new Button{Text="Annulla",DialogResult=DialogResult.Cancel,Location=new Point(492,268),Size=new Size(140,34)};
        apply.Click+=delegate{
            if(adapters.SelectedItem==null)return;
            if(((Adapter)adapters.SelectedItem).Nic.OperationalStatus!=OperationalStatus.Up){MessageBox.Show(this,"Collega il cavo e riapri questa finestra.");return;}
            DialogResult=DialogResult.OK;Close();
        };
        adapters.SelectedIndexChanged+=delegate{UpdateStatus();};
        Controls.AddRange(new Control[]{adapters,pc,status,apply,cancel});CancelButton=cancel;UpdateStatus();
    }
    void UpdateStatus(){
        var a=adapters.SelectedItem as Adapter;
        status.Text=a==null?"Nessuna scheda Ethernet trovata.":"Stato: "+a.Nic.OperationalStatus+"; velocita negoziata: "+(a.Nic.Speed/1e9).ToString("0.##")+" Gbps.\n"+
            (a.Nic.Speed<2500000000L?"Per 2,5 Gbps entrambe le porte devono supportarli.\n":"")+
            "IP dedicato senza gateway; firewall TCP 9911/9912 limitato all'altro PC.";
    }
    public async Task Apply(){
        var nic=((Adapter)adapters.SelectedItem).Nic;
        string guid=new Guid(nic.Id).ToString("D");
        string script=Path.Combine(Path.GetTempPath(),"ds4-ethernet-"+Guid.NewGuid().ToString("N")+".ps1");
        using(var src=Assembly.GetExecutingAssembly().GetManifestResourceStream("DS4.Ethernet.ps1"))
        using(var dst=File.Create(script))src.CopyTo(dst);
        try{
            var info=new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -File \""+script+"\" -AdapterGuid "+guid+" -PcNumber "+PcNumber){UseShellExecute=true,Verb="runas"};
            using(var process=Process.Start(info)){
                await Task.Run(()=>process.WaitForExit());
                if(process.ExitCode!=0)throw new Exception("Configurazione Ethernet non completata. Leggi l'errore mostrato nella console; gli IP del launcher non sono stati cambiati.");
            }
        }finally{File.Delete(script);}
    }
}

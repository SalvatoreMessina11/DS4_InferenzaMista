using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

partial class DS4Launcher {
    ComboBox model = new ComboBox();
    TextBox qwenFolder = new TextBox();
    NumericUpDown gpuLayers = new NumericUpDown();
    bool Qwen { get { return model.SelectedIndex == 1; } }
    string PiModel { get { return Qwen ? "qwen/qwen3.8-27b" : "ds4/deepseek-v4-flash"; } }
    string PiOptions { get { return " --context " + ContextValue + " --max-output " + OutputValue + (Qwen ? " --model qwen" : ""); } }
    void InitModelControls() {
        model.DropDownStyle=ComboBoxStyle.DropDownList;
        model.Items.AddRange(new object[]{"DeepSeek V4 Flash (DS4)","Qwen3.8 27B IQ3_S (llama.cpp)"});
        model.SelectedIndex=1;
        qwenFolder.Text="/home/ds4/aiutante-qwen";
        gpuLayers.Minimum=-1;gpuLayers.Maximum=65;gpuLayers.Value=-1;
    }
    void PlaceModelControls() {
        foreach(Control c in Controls)if(c.Top>=98)c.Top+=42;
        Row("Modello",model,98);
        qwenFolder.Bounds=folder.Bounds;Controls.Add(qwenFolder);
        foreach(Control c in Controls)if(c.Top>=466)c.Top+=42;
        Row("Qwen: livelli GPU (-1 = automatico)",gpuLayers,466);
        ClientSize=new Size(820,1041);
        model.SelectedIndexChanged+=delegate{RefreshContextPresets();Summary();};
        RefreshContextPresets();
        new ToolTip().SetToolTip(gpuLayers,"Qwen ha 64 livelli + output. -1 lascia al backend la scelta in base alla VRAM; 0 usa la CPU. Il resto rimane in RAM. In RPC il totale GPU e distribuito fra i PC.");
        Summary();
    }
    void ModelState() {
        model.Enabled=!busy;
        qwenFolder.Visible=Qwen;folder.Visible=!Qwen;
        qwenFolder.Enabled=!busy;gpuLayers.Enabled=Qwen&&!busy&&role.SelectedIndex!=1;
        keepRam.Enabled=!busy&&!Qwen;
        if(!Qwen)return;
        customSplit.Enabled=layers.Enabled=keepRam.Enabled=false;
        contextNote.Text="Qwen: context nativo massimo 262144; 64 livelli + output. La memoria disponibile limita il context praticabile.";
        summary.Text=(Solo?"Qwen locale RAM + VRAM.":role.SelectedIndex==1?"Worker RPC: solo calcolo; GGUF richiesto solo sul coordinatore.":"Coordinatore Qwen: GPU locale + GPU remota via RPC; prompt solo qui.")+"\r\n64 livelli + output; ripartizione automatica secondo VRAM libera. Output max: "+OutputValue+" token.";
    }
    string QwenResolve() {
        return "set -e\nroot="+Sh(qwenFolder.Text)+"\ncd \"$root\"\n"+
            "test -x build/bin/"+(role.SelectedIndex==1?"ggml-rpc-server":PiMode?"llama-server":"llama-cli")+" || { echo 'Backend Qwen mancante: esegui Quen 27B/setup.sh'; exit 1; }\n"+
            (role.SelectedIndex==1?"":"test -s model.gguf || { echo 'Qwen model.gguf mancante o download incompleto'; exit 1; }\n");
    }
    string QwenPreflight(bool launching) {
        string s=QwenResolve()+"nvidia-smi --query-gpu=name,memory.total,memory.used --format=csv\nfree -h\n";
        if(!Solo)s+="test \"$(wslinfo --networking-mode)\" = mirrored || { echo 'Configura WSL mirrored prima del collegamento LAN'; exit 1; }\nip -4 -o addr show | grep -F "+Sh(" "+local.Text+"/")+" >/dev/null || { echo 'IP locale non presente in Ubuntu'; exit 1; }\n";
        if(launching)s+="if pgrep -x 'ds4|ds4-server|llama-cli|llama-server|ggml-rpc-server' >/dev/null; then echo 'Un modello o worker e gia attivo: chiudi prima la sessione precedente'; exit 1; fi\n";
        if(launching&&(role.SelectedIndex==1||PiMode))s+="test -z \"$(ss -ltnH 'sport = :"+(role.SelectedIndex==1?"9912":"8000")+"')\" || { echo 'Porta gia occupata'; exit 1; }\n";
        return s+(PiMode?PiCheck():"")+"echo VERIFICA_OK\n";
    }
    string QwenCommand() {
        if(role.SelectedIndex==1)return "./build/bin/ggml-rpc-server --device CUDA0 --host "+local.Text+" --port 9912";
        return "./build/bin/"+(PiMode?"llama-server":"llama-cli")+" -m model.gguf --ctx-size "+ContextValue+" --predict "+OutputValue+
            " --load-mode mlock --lazy-mode off --flash-attn on --cache-type-k q8_0 --cache-type-v q8_0 --fit on --fit-target 1024"+
            (gpuLayers.Value<0?"":" --gpu-layers "+(int)gpuLayers.Value)+
            (Solo?"":" --rpc "+peer.Text+":9912 --split-mode layer")+
            (PiMode?" --host 127.0.0.1 --port 8000 --alias qwen3.8-27b --parallel 1":" --conversation");
    }
}

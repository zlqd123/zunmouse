using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MouseKeyboardClicker
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool MessageBeep(uint uType);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint MB_ICONASTERISK = 0x00000040;

        private const int HOTKEY_BASE = 100;
        private const int HOTKEY_EMERGENCY = 99;

        // 修饰键枚举
        [Flags]
        private enum ModifierKey
        {
            None = 0,
            Alt = 0x0001,
            Ctrl = 0x0002,
            Shift = 0x0004
        }

        // 按键控件数据
        private class KeyControlData
        {
            public Button? BtnControl { get; set; }
            public Label? LblStatus { get; set; }
            public ComboBox? CbMode { get; set; }
            public ComboBox? CbKey { get; set; }
            public NumericUpDown? NumFreq { get; set; }
            public Label? LblHotkeyHint { get; set; }
        }

        // 按键数据结构
        private class KeyAction
        {
            public string Name { get; set; } = "";
            public string Mode { get; set; } = "连点";
            public string KeyValue { get; set; } = "左键";
            public int Frequency { get; set; } = 10;
            public bool IsActive { get; set; } = false;
            public Thread? ClickThread { get; set; }
            public CancellationTokenSource? CancelToken { get; set; }
            public Keys Hotkey { get; set; } = Keys.None;
            public string HotkeyBase { get; set; } = "";
            public KeyControlData? Controls { get; set; }
        }

        private List<KeyAction> actions = new List<KeyAction>();
        private bool soundEnabled = true;
        private ModifierKey currentModifier = ModifierKey.Ctrl; // 默认 Ctrl

        private Label? lblMousePos;
        private Button? btnEmergencyStop;
        private CheckBox? chkSound;
        private ComboBox? cbModifier;
        private Label? lblModifierHint;
        private System.Windows.Forms.Timer? mousePosTimer;

        public Form1()
        {
            InitializeActions();
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void InitializeActions()
        {
            actions.Add(new KeyAction { Name = "按键1", Hotkey = Keys.F6, HotkeyBase = "F6" });
            actions.Add(new KeyAction { Name = "按键2", Hotkey = Keys.F7, HotkeyBase = "F7" });
            actions.Add(new KeyAction { Name = "按键3", Hotkey = Keys.F8, HotkeyBase = "F8" });
            actions.Add(new KeyAction { Name = "按键4", Hotkey = Keys.F9, HotkeyBase = "F9" });
            actions.Add(new KeyAction { Name = "按键5", Hotkey = Keys.F10, HotkeyBase = "F10" });
        }

        private void InitializeComponent()
        {
            this.Size = new Size(650, 950);
            this.Text = "多按键连点/长按器 (可自定义修饰键)";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            int yPos = 20;

            // 鼠标坐标
            lblMousePos = new Label
            {
                Text = "当前鼠标坐标: (0, 0)",
                Location = new Point(20, yPos),
                Size = new Size(300, 30),
                Font = new Font("微软雅黑", 11, FontStyle.Bold),
                ForeColor = Color.Blue
            };
            this.Controls.Add(lblMousePos);
            yPos += 40;

            // 全局设置区域
            var gbGlobal = new GroupBox
            {
                Text = "全局设置",
                Location = new Point(20, yPos),
                Size = new Size(600, 90)
            };
            
            chkSound = new CheckBox
            {
                Text = "启用操作音效",
                Location = new Point(20, 25),
                Size = new Size(120, 25),
                Checked = true
            };
            chkSound.CheckedChanged += (s, e) => soundEnabled = chkSound.Checked;
            
            // 修饰键选择
            Label lblModifier = new Label
            {
                Text = "修饰键:",
                Location = new Point(20, 55),
                Size = new Size(60, 25)
            };
            cbModifier = new ComboBox
            {
                Location = new Point(80, 53),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbModifier.Items.AddRange(new object[] { "无", "Ctrl", "Alt", "Shift", "Ctrl+Alt", "Ctrl+Shift", "Alt+Shift", "Ctrl+Alt+Shift" });
            cbModifier.SelectedIndex = 1; // 默认 Ctrl
            cbModifier.SelectedIndexChanged += ModifierChanged;
            
            lblModifierHint = new Label
            {
                Text = "当前热键组合: Ctrl + F6~F10",
                Location = new Point(200, 55),
                Size = new Size(300, 25),
                Font = new Font("微软雅黑", 9, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            
            btnEmergencyStop = new Button
            {
                Text = "关闭所有",
                Location = new Point(450, 25),
                Size = new Size(120, 50),
                BackColor = Color.LightCoral,
                Font = new Font("微软雅黑", 10, FontStyle.Bold)
            };
            btnEmergencyStop.Click += BtnEmergencyStop_Click;
            
            gbGlobal.Controls.Add(chkSound);
            gbGlobal.Controls.Add(lblModifier);
            gbGlobal.Controls.Add(cbModifier);
            gbGlobal.Controls.Add(lblModifierHint);
            gbGlobal.Controls.Add(btnEmergencyStop);
            this.Controls.Add(gbGlobal);
            yPos += 100;

            // 创建5个按键面板
            for (int i = 0; i < actions.Count; i++)
            {
                var panel = CreateKeyPanel(actions[i], i);
                panel.Location = new Point(20, yPos);
                this.Controls.Add(panel);
                yPos += panel.Height + 10;
            }

            mousePosTimer = new System.Windows.Forms.Timer();
            mousePosTimer.Interval = 50;
            mousePosTimer.Tick += MousePosTimer_Tick;
        }

        private void ModifierChanged(object? sender, EventArgs e)
        {
            int selectedIndex = cbModifier?.SelectedIndex ?? 1;
            currentModifier = selectedIndex switch
            {
                0 => ModifierKey.None,
                1 => ModifierKey.Ctrl,
                2 => ModifierKey.Alt,
                3 => ModifierKey.Shift,
                4 => ModifierKey.Ctrl | ModifierKey.Alt,
                5 => ModifierKey.Ctrl | ModifierKey.Shift,
                6 => ModifierKey.Alt | ModifierKey.Shift,
                7 => ModifierKey.Ctrl | ModifierKey.Alt | ModifierKey.Shift,
                _ => ModifierKey.Ctrl
            };
            
            // 更新提示文字
            string modifierText = GetModifierText();
            if (lblModifierHint != null)
                lblModifierHint.Text = $"当前热键组合: {modifierText} + F6~F10 (关闭所有: {modifierText} + F1)";
            
            // 更新每个按键面板的热键提示
            foreach (var action in actions)
            {
                if (action.Controls?.LblHotkeyHint != null)
                {
                    action.Controls.LblHotkeyHint.Text = $"热键: {modifierText} + {action.HotkeyBase}";
                }
            }
            
            // 重新注册热键
            if (this.IsHandleCreated)
            {
                UnregisterHotkeys();
                RegisterHotkeys();
            }
        }

        private string GetModifierText()
        {
            if (currentModifier == ModifierKey.None) return "";
            string text = "";
            if ((currentModifier & ModifierKey.Ctrl) != 0) text += "Ctrl+";
            if ((currentModifier & ModifierKey.Alt) != 0) text += "Alt+";
            if ((currentModifier & ModifierKey.Shift) != 0) text += "Shift+";
            return text.TrimEnd('+');
        }

        private uint GetModifierValue()
        {
            return (uint)currentModifier;
        }

        private GroupBox CreateKeyPanel(KeyAction action, int index)
        {
            var panel = new GroupBox
            {
                Text = $"{action.Name}",
                Size = new Size(600, 150),
                BackColor = Color.WhiteSmoke
            };

            int x = 15;
            int y = 25;
            
            var controls = new KeyControlData();

            // 热键提示
            string modifierText = GetModifierText();
            controls.LblHotkeyHint = new Label
            {
                Text = $"热键: {modifierText} + {action.HotkeyBase}",
                Location = new Point(x + 350, y),
                Size = new Size(200, 25),
                Font = new Font("微软雅黑", 9, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(controls.LblHotkeyHint);

            // 模式选择
            Label lblMode = new Label { Text = "模式:", Location = new Point(x, y), Size = new Size(40, 25) };
            controls.CbMode = new ComboBox
            {
                Location = new Point(x + 45, y),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            controls.CbMode.Items.AddRange(new object[] { "连点", "长按" });
            controls.CbMode.SelectedIndex = action.Mode == "连点" ? 0 : 1;
            controls.CbMode.SelectedIndexChanged += (s, e) =>
            {
                action.Mode = controls.CbMode.SelectedItem?.ToString() ?? "连点";
                UpdatePanelUI(action);
                if (action.IsActive) StopAction(action);
            };
            panel.Controls.Add(lblMode);
            panel.Controls.Add(controls.CbMode);

            y += 35;

            // 按键选择（整合鼠标和键盘）
            Label lblKey = new Label { Text = "按键:", Location = new Point(x, y), Size = new Size(40, 25) };
            controls.CbKey = new ComboBox
            {
                Location = new Point(x + 45, y),
                Size = new Size(220, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            // 添加带分类的按键选项
            controls.CbKey.Items.Add("━━━ 鼠标 ━━━");
            controls.CbKey.Items.Add("左键");
            controls.CbKey.Items.Add("右键");
            controls.CbKey.Items.Add("中键");
            controls.CbKey.Items.Add("━━━ 字母 ━━━");
            for (char c = 'A'; c <= 'Z'; c++)
                controls.CbKey.Items.Add(c.ToString());
            controls.CbKey.Items.Add("━━━ 数字 ━━━");
            for (int i = 0; i <= 9; i++)
                controls.CbKey.Items.Add(i.ToString());
            controls.CbKey.Items.Add("━━━ 符号 ━━━");
            controls.CbKey.Items.Add("Space"); controls.CbKey.Items.Add("Enter"); controls.CbKey.Items.Add("Tab");
            controls.CbKey.Items.Add("Backspace"); controls.CbKey.Items.Add("Escape");
            controls.CbKey.Items.Add("Minus"); controls.CbKey.Items.Add("Equals");
            controls.CbKey.Items.Add("LeftBracket"); controls.CbKey.Items.Add("RightBracket");
            controls.CbKey.Items.Add("Semicolon"); controls.CbKey.Items.Add("Quote");
            controls.CbKey.Items.Add("Comma"); controls.CbKey.Items.Add("Period");
            controls.CbKey.Items.Add("Slash"); controls.CbKey.Items.Add("Backslash");
            controls.CbKey.Items.Add("Tilde");
            controls.CbKey.Items.Add("━━━ 功能键 ━━━");
            for (int i = 1; i <= 12; i++)
                controls.CbKey.Items.Add($"F{i}");
            controls.CbKey.Items.Add("━━━ 方向键 ━━━");
            controls.CbKey.Items.Add("Left"); controls.CbKey.Items.Add("Right");
            controls.CbKey.Items.Add("Up"); controls.CbKey.Items.Add("Down");
            controls.CbKey.Items.Add("━━━ 编辑键 ━━━");
            controls.CbKey.Items.Add("Home"); controls.CbKey.Items.Add("End");
            controls.CbKey.Items.Add("PageUp"); controls.CbKey.Items.Add("PageDown");
            controls.CbKey.Items.Add("Insert"); controls.CbKey.Items.Add("Delete");
            controls.CbKey.Items.Add("━━━ 小键盘 ━━━");
            controls.CbKey.Items.Add("NumLock"); controls.CbKey.Items.Add("Divide");
            controls.CbKey.Items.Add("Multiply"); controls.CbKey.Items.Add("Subtract");
            controls.CbKey.Items.Add("Add"); controls.CbKey.Items.Add("Decimal");
            for (int i = 0; i <= 9; i++)
                controls.CbKey.Items.Add($"NumPad{i}");
            
            controls.CbKey.SelectedItem = action.KeyValue;
            controls.CbKey.SelectedIndexChanged += (s, e) => 
            {
                string selected = controls.CbKey.SelectedItem?.ToString() ?? "左键";
                if (!selected.StartsWith("━━━"))
                    action.KeyValue = selected;
                if (action.IsActive) StopAction(action);
            };
            panel.Controls.Add(lblKey);
            panel.Controls.Add(controls.CbKey);

            y += 35;

            // 频率设置
            Label lblFreq = new Label { Text = "频率(次/秒):", Location = new Point(x, y), Size = new Size(80, 25) };
            controls.NumFreq = new NumericUpDown
            {
                Location = new Point(x + 85, y),
                Size = new Size(60, 25),
                Minimum = 1,
                Maximum = 100,
                Value = action.Frequency
            };
            controls.NumFreq.ValueChanged += (s, e) => action.Frequency = (int)controls.NumFreq.Value;
            panel.Controls.Add(lblFreq);
            panel.Controls.Add(controls.NumFreq);

            // 状态显示
            controls.LblStatus = new Label
            {
                Text = "● 空闲",
                Location = new Point(x + 200, y),
                Size = new Size(100, 25),
                ForeColor = Color.Green,
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };
            panel.Controls.Add(controls.LblStatus);

            // 手动控制按钮
            controls.BtnControl = new Button
            {
                Text = "启动",
                Location = new Point(x + 350, y - 10),
                Size = new Size(80, 35),
                BackColor = Color.LightGreen,
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };
            controls.BtnControl.Click += (s, e) => ToggleAction(action);
            panel.Controls.Add(controls.BtnControl);

            action.Controls = controls;
            
            UpdatePanelUI(action);
            return panel;
        }

        private void UpdatePanelUI(KeyAction action)
        {
            if (action.Controls == null) return;
            
            if (action.Controls.NumFreq != null)
                action.Controls.NumFreq.Enabled = action.Mode == "连点";
        }

        private bool IsMouseKey(string key)
        {
            return key == "左键" || key == "右键" || key == "中键";
        }

        private Keys GetMouseKeyFromName(string keyName)
        {
            return keyName == "右键" ? Keys.RButton : keyName == "中键" ? Keys.MButton : Keys.LButton;
        }

        private Keys GetKeyboardKeyFromName(string keyName)
        {
            return keyName switch
            {
                "Space" => Keys.Space,
                "Enter" => Keys.Enter,
                "Tab" => Keys.Tab,
                "Backspace" => Keys.Back,
                "Escape" => Keys.Escape,
                "Left" => Keys.Left,
                "Right" => Keys.Right,
                "Up" => Keys.Up,
                "Down" => Keys.Down,
                "Home" => Keys.Home,
                "End" => Keys.End,
                "PageUp" => Keys.PageUp,
                "PageDown" => Keys.PageDown,
                "Insert" => Keys.Insert,
                "Delete" => Keys.Delete,
                "Minus" => Keys.OemMinus,
                "Equals" => Keys.Oemplus,
                "LeftBracket" => Keys.OemOpenBrackets,
                "RightBracket" => Keys.OemCloseBrackets,
                "Semicolon" => Keys.OemSemicolon,
                "Quote" => Keys.OemQuotes,
                "Comma" => Keys.Oemcomma,
                "Period" => Keys.OemPeriod,
                "Slash" => Keys.OemQuestion,
                "Backslash" => Keys.OemBackslash,
                "Tilde" => Keys.Oemtilde,
                "NumLock" => Keys.NumLock,
                "Divide" => Keys.Divide,
                "Multiply" => Keys.Multiply,
                "Subtract" => Keys.Subtract,
                "Add" => Keys.Add,
                "Decimal" => Keys.Decimal,
                _ when keyName.StartsWith("NumPad") => 
                    keyName == "NumPad0" ? Keys.NumPad0 :
                    keyName == "NumPad1" ? Keys.NumPad1 :
                    keyName == "NumPad2" ? Keys.NumPad2 :
                    keyName == "NumPad3" ? Keys.NumPad3 :
                    keyName == "NumPad4" ? Keys.NumPad4 :
                    keyName == "NumPad5" ? Keys.NumPad5 :
                    keyName == "NumPad6" ? Keys.NumPad6 :
                    keyName == "NumPad7" ? Keys.NumPad7 :
                    keyName == "NumPad8" ? Keys.NumPad8 :
                    Keys.NumPad9,
                _ when keyName.StartsWith("F") && keyName.Length <= 3 => 
                    (Keys)Enum.Parse(typeof(Keys), keyName),
                _ when keyName.Length == 1 && char.IsDigit(keyName[0]) => 
                    (Keys)Enum.Parse(typeof(Keys), "D" + keyName),
                _ when keyName.Length == 1 && char.IsLetter(keyName[0]) => 
                    (Keys)Enum.Parse(typeof(Keys), keyName),
                _ => Keys.A
            };
        }

        private void PlaySound()
        {
            if (soundEnabled)
            {
                MessageBeep(MB_ICONASTERISK);
            }
        }

        private void PerformMouseDown(int button)
        {
            Keys mouseKey = GetMouseKeyFromName(button == 0 ? "左键" : button == 1 ? "右键" : "中键");
            if (mouseKey == Keys.LButton) mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            else if (mouseKey == Keys.RButton) mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            else mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
        }

        private void PerformMouseUp(int button)
        {
            Keys mouseKey = GetMouseKeyFromName(button == 0 ? "左键" : button == 1 ? "右键" : "中键");
            if (mouseKey == Keys.LButton) mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            else if (mouseKey == Keys.RButton) mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            else mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
        }

        private void PerformKeyDown(Keys key) => keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        private void PerformKeyUp(Keys key) => keybd_event((byte)key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        private void StartAction(KeyAction action)
        {
            if (action.IsActive) return;
            
            action.CancelToken = new CancellationTokenSource();
            var token = action.CancelToken.Token;
            bool isMouse = IsMouseKey(action.KeyValue);
            
            if (action.Mode == "连点")
            {
                action.ClickThread = new Thread(() =>
                {
                    int interval = (int)(1000.0 / action.Frequency);
                    var stopwatch = new Stopwatch();
                    
                    while (!token.IsCancellationRequested)
                    {
                        stopwatch.Restart();
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                if (isMouse)
                                {
                                    int btn = action.KeyValue == "右键" ? 1 : action.KeyValue == "中键" ? 2 : 0;
                                    PerformMouseDown(btn);
                                    Thread.Sleep(5);
                                    PerformMouseUp(btn);
                                }
                                else
                                {
                                    Keys key = GetKeyboardKeyFromName(action.KeyValue);
                                    PerformKeyDown(key);
                                    Thread.Sleep(5);
                                    PerformKeyUp(key);
                                }
                            }));
                        }
                        catch { }
                        
                        int elapsed = (int)stopwatch.ElapsedMilliseconds;
                        int sleepTime = interval - elapsed;
                        if (sleepTime > 0) Thread.Sleep(sleepTime);
                    }
                }) { IsBackground = true };
                action.ClickThread.Start();
            }
            else // 长按模式
            {
                if (isMouse)
                {
                    int btn = action.KeyValue == "右键" ? 1 : action.KeyValue == "中键" ? 2 : 0;
                    PerformMouseDown(btn);
                }
                else
                {
                    PerformKeyDown(GetKeyboardKeyFromName(action.KeyValue));
                }
            }
            
            action.IsActive = true;
            if (action.Controls?.BtnControl != null)
            {
                action.Controls.BtnControl.Text = "停止";
                action.Controls.BtnControl.BackColor = Color.LightCoral;
            }
            if (action.Controls?.LblStatus != null)
            {
                action.Controls.LblStatus.Text = action.Mode == "连点" ? "● 连点中" : "● 长按中";
                action.Controls.LblStatus.ForeColor = Color.Red;
            }
            PlaySound();
        }

        private void StopAction(KeyAction action)
        {
            if (!action.IsActive) return;
            
            bool isMouse = IsMouseKey(action.KeyValue);
            
            if (action.Mode == "连点")
            {
                action.CancelToken?.Cancel();
                action.ClickThread?.Join(500);
            }
            else // 长按模式
            {
                if (isMouse)
                {
                    int btn = action.KeyValue == "右键" ? 1 : action.KeyValue == "中键" ? 2 : 0;
                    PerformMouseUp(btn);
                }
                else
                {
                    PerformKeyUp(GetKeyboardKeyFromName(action.KeyValue));
                }
            }
            
            action.IsActive = false;
            action.CancelToken?.Dispose();
            
            if (action.Controls?.BtnControl != null)
            {
                action.Controls.BtnControl.Text = "启动";
                action.Controls.BtnControl.BackColor = Color.LightGreen;
            }
            if (action.Controls?.LblStatus != null)
            {
                action.Controls.LblStatus.Text = "● 空闲";
                action.Controls.LblStatus.ForeColor = Color.Green;
            }
            PlaySound();
        }

        private void ToggleAction(KeyAction action)
        {
            if (action.IsActive)
                StopAction(action);
            else
                StartAction(action);
        }

        private void EmergencyReleaseAll()
        {
            foreach (var action in actions)
            {
                if (action.IsActive)
                    StopAction(action);
            }
            mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_RIGHTUP | MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
            PlaySound();
        }

        private void BtnEmergencyStop_Click(object? sender, EventArgs e) => EmergencyReleaseAll();

        private void MousePosTimer_Tick(object? sender, EventArgs e)
        {
            if (lblMousePos != null)
            {
                Point pos = Cursor.Position;
                lblMousePos.Text = $"当前鼠标坐标: ({pos.X}, {pos.Y})";
            }
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            mousePosTimer?.Start();
            RegisterHotkeys();
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            EmergencyReleaseAll();
            UnregisterHotkeys();
            mousePosTimer?.Stop();
        }

        private void RegisterHotkeys()
        {
            uint modifier = GetModifierValue();
            
            // 注册关闭所有热键（修饰键 + F1）
            RegisterHotKey(this.Handle, HOTKEY_EMERGENCY, modifier, (uint)Keys.F1);
            
            // 注册 F6 到 F10
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                uint key = (uint)action.Hotkey;
                RegisterHotKey(this.Handle, HOTKEY_BASE + i, modifier, key);
            }
        }

        private void UnregisterHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_EMERGENCY);
            for (int i = 0; i < actions.Count; i++)
            {
                UnregisterHotKey(this.Handle, HOTKEY_BASE + i);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const int WM_HOTKEY = 0x0312;
            
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_EMERGENCY)
                {
                    EmergencyReleaseAll();
                }
                else if (id >= HOTKEY_BASE && id < HOTKEY_BASE + actions.Count)
                {
                    int idx = id - HOTKEY_BASE;
                    ToggleAction(actions[idx]);
                }
            }
        }
    }
}
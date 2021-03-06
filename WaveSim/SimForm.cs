﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WaveSimLib.Code.Visualisation;
using WaveSimLib.Code.Wave;

namespace WaveSim
{
    public partial class SimForm : Form
    {
        private WaveEngine Engine;
        private DynamicColorVisualizer dcv;
        private int _resDiv = 4;

        private int _mouseX, _mouseY;
        private bool _mouseDown;
        private string _fileToLoad = "";

        public SimForm()
        {
            InitializeComponent();
        }

        public SimForm(string filename)
        {
            InitializeComponent();
            _fileToLoad = filename;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Engine = new WaveEngine();
            Engine.OnNewSimulationFrame += new WaveEngine.NewSimulationFrameHandler(Engine_OnNewSimulationFrame);
            Engine.Init(pb_image.Width / _resDiv, pb_image.Height / _resDiv);

            dcv = new DynamicColorVisualizer();

            tsCbMouseAction.SelectedIndex = 0;
            tsCbMouseValue.SelectedIndex = 9;
            tsCbSimResolution.SelectedIndex = 3;

            if (_fileToLoad != "")
            {
                WaveSettings set = Engine.Settings;

                bool result = set.LoadFromFile(_fileToLoad);

                if (!result)
                {
                    MessageBox.Show("An error occured whilst loading the file!", "Load");
                    return;
                }

                //Resize window
                int deltaWidth = this.Width - pb_image.Width;
                int deltaHeight = this.Height - pb_image.Height;

                Size windowS = new Size(set.Width * _resDiv + deltaWidth, set.Height * _resDiv + deltaHeight);
                this.Size = windowS;

                Engine.Settings = set;
                //MessageBox.Show("File loaded successfully!", "Load");
            }

            String PersonalFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveFileDialog.InitialDirectory = PersonalFolder + "\\WaveSim";
            openFileDialog.InitialDirectory = PersonalFolder + "\\WaveSim";

            Engine.Start();
        }

        void Engine_OnNewSimulationFrame(double[,] positionMap, bool[,] wallMap, bool[,] addonWallMap, double[,] massMap, double[,] addonMassMap, int fps)
        {
            Bitmap bmp = dcv.VisualizePositionMap(positionMap, wallMap, addonWallMap, massMap, addonMassMap, Engine.Width, Engine.Height);

            try
            {
                this.Invoke((Action) (() =>
                                          {
                                              pb_image.BackgroundImage = bmp;
                                              SetMouseInfoText(fps);
                                          }));
            }
            catch(Exception) {}
        }

        private void pb_image_Click(object sender, EventArgs e)
        {
            
        }

        private void pb_image_MouseClick(object sender, MouseEventArgs e)
        {
            _mouseX = e.Location.X / _resDiv;
            _mouseY = e.Location.Y / _resDiv;

            DoMouseAction();
        }

        private void DoMouseAction()
        {
            int x = _mouseX;
            int y = _mouseY;

            double value = 0;
            if (tsCbMouseValue.SelectedIndex != -1)
                value = Convert.ToDouble(tsCbMouseValue.Items[tsCbMouseValue.SelectedIndex]);

            if (tsCbMouseAction.SelectedIndex == 0)
            {
                Engine.Poke(x, y, value, 0);
                Engine.Poke(x+1, y, value, 0);
                Engine.Poke(x, y+1, value, 0);
                Engine.Poke(x+1, y+1, value, 0);
            }
            else if (tsCbMouseAction.SelectedIndex == 1)
                Engine.SetWall(x, y);
            else if (tsCbMouseAction.SelectedIndex == 2)
            {
                Engine.SetWall(x, y, false);
                Engine.SetWall(x+1, y, false);
                Engine.SetWall(x, y+1, false);
                Engine.SetWall(x+1, y+1, false);
            }
            else if (tsCbMouseAction.SelectedIndex == 3)
            {
                Engine.SetMass(x, y, value);
                Engine.SetMass(x+1, y, value);
                Engine.SetMass(x, y+1, value);
                Engine.SetMass(x+1, y+1, value);
                Engine.SetMass(x, y + 2, value);
                Engine.SetMass(x + 2, y + 2, value);
            }
            else if (tsCbMouseAction.SelectedIndex == 4)
            {
                Engine.SetMass(x, y, 0);
                Engine.SetMass(x+1, y, 0);
                Engine.SetMass(x, y+1, 0);
                Engine.SetMass(x+1, y+1, 0);
                Engine.SetMass(x, y + +2, 0);
                Engine.SetMass(x + 2, y + 2, 0);
            }
            else if (tsCbMouseAction.SelectedIndex == 5)
            {
                SinusWaveSource sws = new SinusWaveSource();
                sws.X = x;
                sws.Y = y;
                sws.Frequency = value / 10.0;

                Engine.AddWaveSoucre(sws);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Engine.Stop();
        }

        private void pb_image_MouseMove(object sender, MouseEventArgs e)
        {
            _mouseX = e.Location.X / _resDiv;
            _mouseY = e.Location.Y / _resDiv;
            SetMouseInfoText(0);
            if (_mouseDown)
                DoMouseAction();
        }

        private void SetMouseInfoText(int fps)
        {
            try
            {
                double v = Engine.GetVelocity(_mouseX, _mouseY);
                double p = Engine.GetPosition(_mouseX, _mouseY);
                double w = Engine.GetEnergy(_mouseX, _mouseY);

                l_mouseinfo.Text = "[" + (_mouseX).ToString() + "|" + (_mouseY).ToString() + "] Elongation: " +
                                   p.ToString("##0.000") + " Velocity: " +
                                   v.ToString("##0.000" + " Energy: " + w.ToString("##0.000") + " FPS: " +
                                              fps.ToString());
            }
            catch (Exception) {}
        }

        private void pb_image_MouseDown(object sender, MouseEventArgs e)
        {
            _mouseDown = true;
            _mouseX = e.Location.X / _resDiv;
            _mouseY = e.Location.Y / _resDiv;
        }

        private void pb_image_MouseUp(object sender, MouseEventArgs e)
        {
            _mouseDown = false;
        }

        private void pb_image_Resize(object sender, EventArgs e)
        {
            //Re-init
            Engine.Stop();
            //Restart is done by Form - resize end
            //Engine.Init(pb_image.Width / _resDiv, pb_image.Height / _resDiv);
        }

        private Size _oldsize;

        private void SimForm_ResizeBegin(object sender, EventArgs e)
        {
            Engine.Stop();
            _oldsize = this.Size;
        }

        private void SimForm_ResizeEnd(object sender, EventArgs e)
        {  
            if(!this.Size.Equals(_oldsize))
            Engine.Init(pb_image.Width / _resDiv, pb_image.Height / _resDiv);

            Engine.Start();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Engine.Stop();
            WaveSettings set = Engine.Settings;

            DialogResult res = openFileDialog.ShowDialog();

            if (res != System.Windows.Forms.DialogResult.Cancel)
            {
                bool result = set.LoadFromFile(openFileDialog.FileName);

                if (!result)
                {
                    MessageBox.Show("An error occured whilst loading the file!", "Load");
                    Engine.Start();
                    return;
                }

                //Resize windows
                int deltaWidth = this.Width - pb_image.Width;
                int deltaHeight = this.Height - pb_image.Height;

                Size windowS = new Size(set.Width * _resDiv + deltaWidth, set.Height * _resDiv + deltaHeight);
                this.Size = windowS;

                Engine.Settings = set;
                MessageBox.Show("File loaded successfully!", "Load");
                Engine.Start();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Engine.Stop();
            WaveSettings set = Engine.Settings;

            DialogResult res = saveFileDialog.ShowDialog();

            if (res != System.Windows.Forms.DialogResult.Cancel)
            {
                bool result = set.SaveToFile(saveFileDialog.FileName);
                if (result)
                {
                    MessageBox.Show("File successfully saved!", "Save");
                }
                else
                {
                    MessageBox.Show("Error whilst saving the file!", "Save");
                }
            }

            Engine.Start();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Engine.Stop();
            this.Close();
            Application.Exit();
        }

        private void resetAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Engine.Init(pb_image.Width / _resDiv, pb_image.Height / _resDiv);
            Engine.Start();
        }

        private void resetElongationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Engine.ResetElongation();
        }

        private void resetMassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Engine.ResetMass();
        }

        private void resetSourcesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Engine.ResetSources();
        }

        private void resetWallsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Engine.ResetWalls();
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Engine.SimulationRunning)
            {
                pauseToolStripMenuItem.Text = "Resume";
                Engine.Stop();
            }
            else
            {
                pauseToolStripMenuItem.Text = "Pause";
                Engine.Start();
            }
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Engine.Stop();
            pauseToolStripMenuItem.Text = "Resume";
            Engine.ManualStep();
        }

        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {
            if (tsCbSimResolution.SelectedIndex != -1)
            {
                _resDiv = tsCbSimResolution.SelectedIndex + 1;
                Engine.Init(pb_image.Width / _resDiv, pb_image.Height / _resDiv);
                Engine.Start();
            }
        }

        private void editSimulationParametersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WaveSettings set = Engine.Settings;

            SimSettingsForm ssf = new SimSettingsForm();
            ssf.Settings = set;
            ssf.ShowDialog();

            if (ssf.Settings != null)
            {
                Engine.Settings = ssf.Settings;
            }
        }

        private void editColorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SimVisForm svf = new SimVisForm(dcv);
            svf.Show();
        }
    }
}

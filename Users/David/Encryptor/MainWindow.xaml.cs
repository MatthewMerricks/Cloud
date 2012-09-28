//
// MainWindow.xaml.cs
// Encryptor
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Encryptor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordField.Password))
            {
                MessageBox.Show("Needs password");
            }
            else if (string.IsNullOrWhiteSpace(InputField.Text))
            {
                MessageBox.Show("Needs input");
            }
            else
            {
                OutputField.Text = EncryptString(InputField.Text, PasswordField.Password);
            }
        }

        #region encrypt/decrypt strings
        // create and initialize a crypto algorithm 
        private static SymmetricAlgorithm getAlgorithm(string password)
        {
            SymmetricAlgorithm algorithm = Rijndael.Create();
            Rfc2898DeriveBytes rdb = new Rfc2898DeriveBytes(
                password, new byte[] { 
                    0x53,0x6f,0x64,0x69,0x75,0x6d,0x20,             // salty goodness 
                    0x43,0x68,0x6c,0x6f,0x72,0x69,0x64,0x65 
                }
            );
            algorithm.Padding = PaddingMode.ISO10126;
            algorithm.Key = rdb.GetBytes(32);
            algorithm.IV = rdb.GetBytes(16);
            return algorithm;
        }

        /*  
         * encryptString 
         * provides simple encryption of a string, with a given password 
         */
        public static string EncryptString(string clearText, string password)
        {
            SymmetricAlgorithm algorithm = getAlgorithm(password);
            byte[] clearBytes = System.Text.Encoding.Unicode.GetBytes(clearText);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, algorithm.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(clearBytes, 0, clearBytes.Length);
            cs.Close();
            return Convert.ToBase64String(ms.ToArray());
        }

        /* 
         * decryptString 
         * provides simple decryption of a string, with a given password 
         */
        public static string DecryptString(string cipherText, string password)
        {
            SymmetricAlgorithm algorithm = getAlgorithm(password);
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, algorithm.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipherBytes, 0, cipherBytes.Length);
            cs.Close();
            return System.Text.Encoding.Unicode.GetString(ms.ToArray());
        }
        #endregion

        private void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordField.Password))
            {
                MessageBox.Show("Needs password");
            }
            else if (string.IsNullOrWhiteSpace(OutputField.Text))
            {
                MessageBox.Show("Needs output");
            }
            else
            {
                InputField.Text = DecryptString(OutputField.Text, PasswordField.Password);
            }
        }
    }
}
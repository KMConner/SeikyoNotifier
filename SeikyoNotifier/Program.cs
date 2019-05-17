using System;
using System.Text;
using static System.Console;

namespace SeikyoNotifier
{
    class Program
    {
        static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string userName, passwd;
            if (args.Length == 2)
            {
                userName = args[0];
                passwd = args[1];
            }
            else
            {
                if (string.IsNullOrEmpty(userName = Environment.GetEnvironmentVariable("COOP_USER")) ||
                    string.IsNullOrEmpty(passwd = Environment.GetEnvironmentVariable("COOP_PASS")))
                {
                    WriteLine("User name and password must be specified.");
                    return -1;
                }
            }
            var controller = new SeikyoController(userName, passwd);
            try
            {
                controller.GetBalance();
            }
            catch (Exception ex)
            {
                WriteLine("ERROR!!!" + ex.Message);
            }
            return 0;
        }
    }
}

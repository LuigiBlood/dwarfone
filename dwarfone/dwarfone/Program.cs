using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dwarfone
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DWARFone --- by LuigiBlood");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dwarfone <elf file>");
            }
            else
            {
                ELF elf = new ELF(args[0]);
                Console.WriteLine("Error Code: " + elf.GetError());
            }
        }
    }
}

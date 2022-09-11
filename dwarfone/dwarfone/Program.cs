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
                Console.WriteLine("Usage: dwarfone [--enable-quirks] <elf file>");
            }
            else
            {
                bool quirksEnabled = args[0] == "--enable-quirks";
                string name = quirksEnabled ? args[1] : args[0];

                ELF elf = new ELF(name);
                if (elf.GetError() != 0)
                    Console.WriteLine("Error Code: " + elf.GetError());
                else
                    Console.WriteLine("ELF file successfully loaded");

                DWARF.DumpDWARF(elf, quirksEnabled);
            }
        }
    }
}

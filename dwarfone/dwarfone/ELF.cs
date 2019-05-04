using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace dwarfone
{
    class ELF
    {
        //Only cares about sections, not program headers, 32-bit ELF only
        class elf_header
        {
            public uint e_hdr_magic;       //Magic number:     0x7F ELF
            public byte e_hdr_class;       //Class:            1 = 32-bit, 2 = 64-bit (unsupported)
            public byte e_hdr_data;        //Endianness:       1 = LE, 2 = BE
            public byte e_hdr_version;     //Version:          Should be 1
            public byte e_hdr_osabi;       //OS ABI:           Usually 0 (System-V)
            public byte e_hdr_abiversion;  //ABI Version
                                           //7 byte padding
            public ushort e_type;          //Object File Type
            public ushort e_machine;       //Target Instruction Set
            public uint e_version;         //Version
            public uint e_entry;           //Address Entrypoint
            public uint e_phoff;           //Program Header Table File Offset
            public uint e_shoff;           //Section Header Table File Offset
            public uint e_flags;           //Flags
            public ushort e_ehsize;        //ELF Header Size
            public ushort e_phentsize;     //Program Header Entry Size
            public ushort e_phnum;         //Amount of Program Header entries
            public ushort e_shentsize;     //Section Header Entry Size
            public ushort e_shnum;         //Amount of Section Header entries
            public ushort e_shstrndx;      //Contains index of the section header table entry that contains the section names.
        };

        class section_header
        {
            public uint sh_name_off;       //Offset to Section Name in .shstrtab section
            public string sh_name;         //Section Name
            public uint sh_type;           //Section Type
            public uint sh_flags;          //Section Flags
            public uint sh_addr;           //Virtual address of section
            public uint sh_offset;         //Section Data File Offset
            public uint sh_size;           //Section Size in bytes
            public uint sh_link;           //Section Index of an associated section
            public uint sh_info;           //Extra Info
            public uint sh_addralign;      //Required alignment of section (power of 2)
            public uint sh_entsize;        //Entry Size (may be 0)
        };

        private elf_header hdr;
        private List<section_header> section_hdrs;
        public byte[] elf;
        private int error;          //For future use in main program, 0 = no error, anything else = error

        //For DWARF v1 parsing later:
        public uint debug_offset;
        public uint debug_size;

        public ELF(string elf_file)
        {
            //Load ELF file
            error = 0;

            FileStream file = new FileStream(elf_file, FileMode.Open);
            elf = new byte[file.Length];
            file.Read(elf, 0, (int)file.Length);
            file.Close();

            //Parse ELF
            //Read Header
            hdr = new elf_header();
            MemoryStream elf_data = new MemoryStream(elf);
            elf_data.Seek(0, SeekOrigin.Begin);
            hdr.e_hdr_magic = ReadUInt32(elf_data, 2);
            hdr.e_hdr_class = elf[4];
            hdr.e_hdr_data = elf[5];
            hdr.e_hdr_version = elf[6];

            if (hdr.e_hdr_magic != 0x7F454C46)
            {
                //Not ELF file
                Console.WriteLine(hdr.e_hdr_magic.ToString("x"));
                error = 1;
                return;
            }

            if (hdr.e_hdr_class != 1)
            {
                //Is not 32-bit ELF
                error = 2;
                return;
            }

            elf_data.Seek(0x18, SeekOrigin.Begin);
            hdr.e_entry = ReadUInt32(elf_data, hdr.e_hdr_data);
            hdr.e_phoff = ReadUInt32(elf_data, hdr.e_hdr_data);
            hdr.e_shoff = ReadUInt32(elf_data, hdr.e_hdr_data);
            hdr.e_flags = ReadUInt32(elf_data, hdr.e_hdr_data);
            hdr.e_ehsize = ReadUInt16(elf_data, hdr.e_hdr_data);
            hdr.e_phentsize = ReadUInt16(elf_data, hdr.e_hdr_data);
            hdr.e_phnum = ReadUInt16(elf_data, hdr.e_hdr_data);
            hdr.e_shentsize = ReadUInt16(elf_data, hdr.e_hdr_data);
            hdr.e_shnum = ReadUInt16(elf_data, hdr.e_hdr_data);
            hdr.e_shstrndx = ReadUInt16(elf_data, hdr.e_hdr_data);

            //Read Section Headers
            section_hdrs = new List<section_header>();
            for (int i = 0; i < hdr.e_shnum; i++)
            {
                elf_data.Seek(hdr.e_shoff + hdr.e_shentsize * i, SeekOrigin.Begin);

                section_header section = new section_header();

                section.sh_name_off = ReadUInt32(elf_data, hdr.e_hdr_data);
                section.sh_type = ReadUInt32(elf_data, hdr.e_hdr_data);
                section.sh_flags = ReadUInt32(elf_data, hdr.e_hdr_data);
                section.sh_addr = ReadUInt32(elf_data, hdr.e_hdr_data);
                section.sh_offset = ReadUInt32(elf_data, hdr.e_hdr_data);
                section.sh_size = ReadUInt32(elf_data, hdr.e_hdr_data);
                section.sh_link = ReadUInt32(elf_data, hdr.e_hdr_data);
                section.sh_info = ReadUInt32(elf_data, hdr.e_hdr_data);
                section.sh_addralign = ReadUInt32(elf_data, hdr.e_hdr_data);
                section.sh_entsize = ReadUInt32(elf_data, hdr.e_hdr_data);

                section_hdrs.Add(section);
            }
            
            //Get Names
            for (int i = 0; i < section_hdrs.Count; i++)
            {
                elf_data.Seek(section_hdrs[hdr.e_shstrndx].sh_offset + section_hdrs[i].sh_name_off, SeekOrigin.Begin);
                section_hdrs[i].sh_name = ReadString(elf_data);
                Console.WriteLine(section_hdrs[i].sh_name + " - Offset: 0x" + section_hdrs[i].sh_offset.ToString("x"));
                if (section_hdrs[i].sh_name == ".debug")
                {
                    debug_offset = section_hdrs[i].sh_offset;
                    debug_size = section_hdrs[i].sh_size;
                }
            }

            elf_data.Close();
        }

        public int GetError()
        {
            return error;
        }

        public int GetEndian()
        {
            return hdr.e_hdr_data;
        }

        static public ulong ReadUInt64(MemoryStream stream, int endian)
        {
            ulong temp = 0;

            for (int i = 0; i < 4; i++)
            {
                if (endian == 1)
                {
                    //LE
                    temp >>= 8;
                    temp |= (uint)(stream.ReadByte() << 24);
                }
                else if (endian == 2)
                {
                    //BE
                    temp <<= 8;
                    temp |= (uint)stream.ReadByte();
                }
            }
            return temp;
        }

        static public uint ReadUInt32(MemoryStream stream, int endian)
        {
            uint temp = 0;

            for (int i = 0; i < 4; i++)
            {
                if (endian == 1)
                {
                    //LE
                    temp >>= 8;
                    temp |= (uint)(stream.ReadByte() << 24);
                }
                else if (endian == 2)
                {
                    //BE
                    temp <<= 8;
                    temp |= (uint)stream.ReadByte();
                }
            }
            return temp;
        }

        static public ushort ReadUInt16(MemoryStream stream, int endian)
        {
            ushort temp = 0;

            for (int i = 0; i < 2; i++)
            {
                if (endian == 1)
                {
                    //LE
                    temp >>= 8;
                    temp |= (ushort)(stream.ReadByte() << 8);
                }
                else if (endian == 2)
                {
                    //BE
                    temp <<= 8;
                    temp |= (ushort)stream.ReadByte();
                }
            }
            return temp;
        }

        static public string ReadString(MemoryStream stream)
        {
            char temp = (char)0;
            string str = "";
            do
            {
                temp = (char)stream.ReadByte();
                if (temp != 0)
                    str += temp.ToString();
            } while (temp != 0);
            return str;
        }
    }
}

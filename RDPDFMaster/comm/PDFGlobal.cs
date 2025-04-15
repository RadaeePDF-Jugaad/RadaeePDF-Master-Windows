using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.Storage;
using RDDLib.comm;
using RDDLib.pdf;
using RDPDFMaster.Modules.FTS;

namespace com.radaee.master
{
    class PDFGlobal
    {
        static private bool ms_loaded = false;
        static private void load_data()
        {
            if (ms_loaded) return;
            ms_loaded = true;
            String inst_path = Package.Current.InstalledLocation.Path;
            String cmap_path = inst_path + "\\Assets\\dat\\cmaps.dat";
            String umap_path = inst_path + "\\Assets\\dat\\umaps.dat";
            String cmyk_path = inst_path + "\\Assets\\dat\\cmyk_rgb.dat";
            if (new FileInfo(cmap_path).Exists && new FileInfo(umap_path).Exists)
                RDGlobal.SetCMapsPath(cmap_path, umap_path);
            if (new FileInfo(cmyk_path).Exists)
                RDGlobal.SetCMYKICC(cmyk_path);

            RDGlobal.FontFileListStart();
            //the new UWP can access font directory in system path.
            String fpath = SystemDataPaths.GetDefault().Fonts;
            DirectoryInfo finfo = new DirectoryInfo(fpath);
            FileInfo[] files = finfo.GetFiles();
            foreach (FileInfo file in files)
            {
                String ext = file.Extension.ToLower();
                if (ext.CompareTo(".ttf") == 0 || ext.CompareTo(".ttc") == 0 ||
                    ext.CompareTo(".otf") == 0 || ext.CompareTo(".otc") == 0)
                    RDGlobal.FontFileListAdd(file.FullName);
            }
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\argbsn00lp.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\arimo.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\arimob.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\arimobi.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\arimoi.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\texgy.otf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\texgyb.otf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\texgybi.otf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\texgyi.otf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\cousine.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\cousineb.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\cousinei.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\cousinebi.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\symbol.ttf");
            RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\amiriRegular.ttf");
            RDGlobal.FontFileListEnd();

            RDGlobal.FontFileMapping("Arial", "Arimo");
            RDGlobal.FontFileMapping("Arial Bold", "Arimo Bold");
            RDGlobal.FontFileMapping("Arial BoldItalic", "Arimo Bold Italic");
            RDGlobal.FontFileMapping("Arial Italic", "Arimo Italic");
            RDGlobal.FontFileMapping("Arial,Bold", "Arimo Bold");
            RDGlobal.FontFileMapping("Arial,BoldItalic", "Arimo Bold Italic");
            RDGlobal.FontFileMapping("Arial,Italic", "Arimo Italic");
            RDGlobal.FontFileMapping("Arial-Bold", "Arimo Bold");
            RDGlobal.FontFileMapping("Arial-BoldItalic", "Arimo Bold Italic");
            RDGlobal.FontFileMapping("Arial-Italic", "Arimo Italic");
            RDGlobal.FontFileMapping("ArialMT", "Arimo");
            RDGlobal.FontFileMapping("Calibri", "Arimo");
            RDGlobal.FontFileMapping("Calibri Bold", "Arimo Bold");
            RDGlobal.FontFileMapping("Calibri BoldItalic", "Arimo Bold Italic");
            RDGlobal.FontFileMapping("Calibri Italic", "Arimo Italic");
            RDGlobal.FontFileMapping("Calibri,Bold", "Arimo Bold");
            RDGlobal.FontFileMapping("Calibri,BoldItalic", "Arimo Bold Italic");
            RDGlobal.FontFileMapping("Calibri,Italic", "Arimo Italic");
            RDGlobal.FontFileMapping("Calibri-Bold", "Arimo Bold");
            RDGlobal.FontFileMapping("Calibri-BoldItalic", "Arimo Bold Italic");
            RDGlobal.FontFileMapping("Calibri-Italic", "Arimo Italic");
            RDGlobal.FontFileMapping("Helvetica", "Arimo");
            RDGlobal.FontFileMapping("Helvetica Bold", "Arimo Bold");
            RDGlobal.FontFileMapping("Helvetica BoldItalic", "Arimo Bold Italic");
            RDGlobal.FontFileMapping("Helvetica Italic", "Arimo Italic");
            RDGlobal.FontFileMapping("Helvetica,Bold", "Arimo,Bold");
            RDGlobal.FontFileMapping("Helvetica,BoldItalic", "Arimo Bold Italic");
            RDGlobal.FontFileMapping("Helvetica,Italic", "Arimo Italic");
            RDGlobal.FontFileMapping("Helvetica-Bold", "Arimo Bold");
            RDGlobal.FontFileMapping("Helvetica-BoldItalic", "Arimo Bold Italic");
            RDGlobal.FontFileMapping("Helvetica-Italic", "Arimo Italic");

            RDGlobal.FontFileMapping("Garamond", "TeXGyreTermes-Regular");
            RDGlobal.FontFileMapping("Garamond,Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("Garamond,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("Garamond,Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("Garamond-Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("Garamond-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("Garamond-Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("Times", "TeXGyreTermes-Regular");
            RDGlobal.FontFileMapping("Times,Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("Times,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("Times,Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("Times-Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("Times-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("Times-Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("Times-Roman", "TeXGyreTermes-Regular");
            RDGlobal.FontFileMapping("Times New Roman", "TeXGyreTermes-Regular");
            RDGlobal.FontFileMapping("Times New Roman,Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("Times New Roman,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("Times New Roman,Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("Times New Roman-Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("Times New Roman-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("Times New Roman-Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("TimesNewRoman", "TeXGyreTermes-Regular");
            RDGlobal.FontFileMapping("TimesNewRoman,Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("TimesNewRoman,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("TimesNewRoman,Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("TimesNewRoman-Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("TimesNewRoman-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("TimesNewRoman-Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("TimesNewRomanPS", "TeXGyreTermes-Regular");
            RDGlobal.FontFileMapping("TimesNewRomanPS,Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("TimesNewRomanPS,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("TimesNewRomanPS,Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("TimesNewRomanPS-Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("TimesNewRomanPS-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("TimesNewRomanPS-Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("TimesNewRomanPSMT", "TeXGyreTermes-Regular");
            RDGlobal.FontFileMapping("TimesNewRomanPSMT,Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("TimesNewRomanPSMT,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("TimesNewRomanPSMT,Italic", "TeXGyreTermes-Italic");
            RDGlobal.FontFileMapping("TimesNewRomanPSMT-Bold", "TeXGyreTermes-Bold");
            RDGlobal.FontFileMapping("TimesNewRomanPSMT-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDGlobal.FontFileMapping("TimesNewRomanPSMT-Italic", "TeXGyreTermes-Italic");

            RDGlobal.FontFileMapping("Courier", "Cousine");
            RDGlobal.FontFileMapping("Courier Bold", "Cousine Bold");
            RDGlobal.FontFileMapping("Courier BoldItalic", "Cousine Bold Italic");
            RDGlobal.FontFileMapping("Courier Italic", "Cousine Italic");
            RDGlobal.FontFileMapping("Courier,Bold", "Cousine Bold");
            RDGlobal.FontFileMapping("Courier,BoldItalic", "Cousine Bold Italic");
            RDGlobal.FontFileMapping("Courier,Italic", "Cousine Italic");
            RDGlobal.FontFileMapping("Courier-Bold", "Cousine Bold");
            RDGlobal.FontFileMapping("Courier-BoldItalic", "Cousine Bold Italic");
            RDGlobal.FontFileMapping("Courier-Italic", "Cousine Italic");
            RDGlobal.FontFileMapping("Courier New", "Cousine");
            RDGlobal.FontFileMapping("Courier New Bold", "Cousine Bold");
            RDGlobal.FontFileMapping("Courier New BoldItalic", "Cousine Bold Italic");
            RDGlobal.FontFileMapping("Courier New Italic", "Cousine Italic");
            RDGlobal.FontFileMapping("Courier New,Bold", "Cousine Bold");
            RDGlobal.FontFileMapping("Courier New,BoldItalic", "Cousine Bold Italic");
            RDGlobal.FontFileMapping("Courier New,Italic", "Cousine Italic");
            RDGlobal.FontFileMapping("Courier New-Bold", "Cousine Bold");
            RDGlobal.FontFileMapping("Courier New-BoldItalic", "Cousine Bold Italic");
            RDGlobal.FontFileMapping("Courier New-Italic", "Cousine Italic");
            RDGlobal.FontFileMapping("CourierNew", "Cousine");
            RDGlobal.FontFileMapping("CourierNew Bold", "Cousine Bold");
            RDGlobal.FontFileMapping("CourierNew BoldItalic", "Cousine Bold Italic");
            RDGlobal.FontFileMapping("CourierNew Italic", "Cousine Italic");
            RDGlobal.FontFileMapping("CourierNew,Bold", "Cousine Bold");
            RDGlobal.FontFileMapping("CourierNew,BoldItalic", "Cousine Bold Italic");
            RDGlobal.FontFileMapping("CourierNew,Italic", "Cousine Italic");
            RDGlobal.FontFileMapping("CourierNew-Bold", "Cousine Bold");
            RDGlobal.FontFileMapping("CourierNew-BoldItalic", "Cousine Bold Italic");
            RDGlobal.FontFileMapping("CourierNew-Italic", "Cousine Italic");

            RDGlobal.FontFileMapping("Symbol", "Symbol Neu for Powerline");
            RDGlobal.FontFileMapping("Symbol,Bold", "Symbol Neu for Powerline");
            RDGlobal.FontFileMapping("Symbol,BoldItalic", "Symbol Neu for Powerline");
            RDGlobal.FontFileMapping("Symbol,Italic", "Symbol Neu for Powerline");

            int face_first = 0;
            int face_count = RDGlobal.GetFaceCount();
            String rand_fname = null;
            uint sys_fonts = 0;
            while (face_first < face_count)
            {
                String fname = RDGlobal.GetFaceName(face_first);
                if (fname != null)
                {
                    if (fname.CompareTo("SimSun") == 0) sys_fonts |= 1;
                    if (fname.CompareTo("Microsoft JHengHei") == 0 || fname.CompareTo("MingLiU") == 0) sys_fonts |= 2;
                    if (fname.CompareTo("MS Gothic") == 0) sys_fonts |= 4;
                    if (fname.CompareTo("Malgun Gothic Regular") == 0) sys_fonts |= 8;
                }
                if (rand_fname == null && fname != null && fname.Length > 0)
                    rand_fname = fname;
                face_first++;
            }
            // set default fonts.
            if (sys_fonts > 0)
            {
                RDGlobal.SetDefaultFont("", "Calibri", true);
                RDGlobal.SetDefaultFont("", "Times New Roman", false);
                RDGlobal.SetDefaultFont("GB1", "SimSun", true);
                RDGlobal.SetDefaultFont("GB1", "SimSun", false);
                if (!RDGlobal.SetDefaultFont("CNS1", "Microsoft JHengHei", true))
                    RDGlobal.SetDefaultFont("CNS1", "MingLiU", true);
                if (!RDGlobal.SetDefaultFont("CNS1", "Microsoft JHengHei", false))
                    RDGlobal.SetDefaultFont("CNS1", "MingLiU", false);
                RDGlobal.SetDefaultFont("Japan1", "MS Gothic", true);
                RDGlobal.SetDefaultFont("Japan1", "MS Gothic", false);
                RDGlobal.SetDefaultFont("Korea1", "Malgun Gothic Regular", true);
                RDGlobal.SetDefaultFont("Korea1", "Malgun Gothic Regular", false);
                RDGlobal.SetAnnotFont("SimSun");
                PDFEditNode.SetDefFont("Times New Roman");
                PDFEditNode.SetDefCJKFont("SimSun");
            }
            else
            {
                if (!RDGlobal.SetDefaultFont("", "AR PL SungtiL GB", true) && rand_fname != null)
                    RDGlobal.SetDefaultFont("", rand_fname, true);
                if (!RDGlobal.SetDefaultFont("", "AR PL SungtiL GB", false) && rand_fname != null)
                    RDGlobal.SetDefaultFont("", rand_fname, false);
                if (!RDGlobal.SetAnnotFont("AR PL SungtiL GB") && rand_fname != null)
                    RDGlobal.SetAnnotFont(rand_fname);
                PDFEditNode.SetDefFont("Arial");
                PDFEditNode.SetDefCJKFont("AR PL SungtiL GB");
            }

            // set annotation text font.
            RDGlobal.LoadStdFont(13, inst_path + "\\Assets\\font\\rdf013");
        }
        static public bool init()
        {
            load_data();
            //String sver = RDGlobal.GetVersion();//this versioin string, example "20220225".
            //the key is binding to package "com.radaee.reader", can active version before "20250518"
            int ret = RDGlobal.ActiveLicense("6804C2339FB1E30E60E3D3E09F430347176D31744B4CA3BC1800AD7ECE827DD26395C6BD9C8AAEFC2D7633969C642CF0");
            //init FTS manager
            RadaeeFTSManager.Init();
            return ret == 3;
        }
    }
}

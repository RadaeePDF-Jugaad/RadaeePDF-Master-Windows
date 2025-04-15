#pragma once
#include "UWPCom.h"
#include <stdlib.h>
#include <windows.h>

using namespace Platform;
using namespace Windows::Graphics::Imaging;
char* cvt_str_cstr(String^ str);
String^ cvt_cstr_str(const char* str);

namespace RDDLib
{
	namespace pdf
	{
		ref class PDFPage;
		ref class PDFAnnot;
		ref class PDFPageContent;
	}
	namespace docx
	{
		ref class DOCXPage;
		ref class DOCXDoc;
	}
	namespace comm
	{
		ref class RDBmp;
		public enum class RD_RENDER_MODE
		{
			mode_poor = 0,
			mode_normal = 1,
			mode_best = 2,
		};
		public enum class RD_ERROR
		{
			err_ok,
			err_invalid_para,
			err_open,
			err_password,
			err_encrypt,
			err_bad_file,
		};
		public value struct RDPoint
		{
			float x;
			float y;
		};
		public value struct RDRect
		{
			float left;
			float top;
			float right;
			float bottom;
		};
		public ref class RDGlobal sealed
		{
		public:
			/// <summary>
			/// Set paths of cmap and umap resources
			/// </summary>
			/// <param name="cpath">Path of cmap</param>
			/// <param name="upath">Path of umap</param>
			static void SetCMapsPath(String^ cpath, String^ upath)
			{
				char* cp = cvt_str_cstr(cpath);
				char* up = cvt_str_cstr(upath);
				Global_setCMapsPath(cp, up);
				free(cp);
				free(up);
			}
			/// <summary>
			/// Set CMYK resource path and load it.
			/// </summary>
			/// <param name="path">Path of CMYK resource</param>
			/// <returns>True if successed otherwise false</returns>
			static Boolean SetCMYKICC(String^ path)
			{
				char* cp = cvt_str_cstr(path);
				bool ret = Global_setCMYKICC(cp);
				free(cp);
				return ret;
			}
			/// <summary>
			/// Start to load font files. All font files will be loaded into a font list.
			/// </summary>
			static void FontFileListStart()
			{
				Global_fontfileListStart();
			}
			/// <summary>
			/// Add specified font file into font list
			/// </summary>
			/// <param name="path">Path of the font file</param>
			static void FontFileListAdd(String^ path)
			{
				char* cp = cvt_str_cstr(path);
				Global_fontfileListAdd(cp);
				free(cp);
			}
			/// <summary>
			/// End adding font files to font list
			/// </summary>
			static void FontFileListEnd()
			{
				Global_fontfileListEnd();
			}
			/// <summary>
			/// Map a font with a different font name. If the original font resource with that font name is missing the mapped font will be used as a substitute
			/// </summary>
			/// <param name="map_name">A different font name</param>
			/// <param name="name">The name of the font which will be mapped</param>
			/// <returns>True if successed, otherwise false</returns>
			static bool FontFileMapping(String^ map_name, String^ name)
			{
				char* mname = cvt_str_cstr(map_name);
				char* dname = cvt_str_cstr(name);
				bool ret = Global_fontfileMapping(mname, dname);
				free(mname);
				free(dname);
				return ret;
			}
			/// <summary>
			/// Get available font count
			/// </summary>
			/// <returns>Available font count</returns>
			static int GetFaceCount()
			{
				return Global_getFaceCount();
			}
			/// <summary>
			/// Get font face name at specified index position
			/// </summary>
			/// <param name="index">The index position of the font</param>
			/// <returns>Name of the font face if found</returns>
			static String^ GetFaceName(int index)
			{
				return cvt_cstr_str(Global_getFaceName(index));
			}
			/// <summary>
			/// Set a font as default font for specified character set
			/// </summary>
			/// <param name="collection">Collection name of the character set, e.g. GB1, Japan1, Korea1, etc.</param>
			/// <param name="name">Name of the font which will be set as default font</param>
			/// <param name="fixed">If the font will be used as fixed default font</param>
			/// <returns>True if successed, otherwise false</returns>
			static Boolean SetDefaultFont(String^ collection, String^ name, Boolean fixed)
			{
				char* cc = cvt_str_cstr(collection);
				char* cn = cvt_str_cstr(name);
				bool ret = Global_setDefaultFont(cc, cn, fixed);
				free(cc);
				free(cn);
				return ret;
			}
			/// <summary>
			/// Load standard font
			/// </summary>
			/// <param name="index">Index of the font</param>
			/// <param name="path">Path of the font file</param>
			static void LoadStdFont(int index, String^ path)
			{
				char* cp = cvt_str_cstr(path);
				Global_loadStdFont(index, cp);
				free(cp);
			}
			/// <summary>
			/// Set font for annotations
			/// </summary>
			/// <param name="name">Name of the font</param>
			/// <returns>True if successed, otherwise false</returns>
			static Boolean SetAnnotFont(String^ name)
			{
				char* cn = cvt_str_cstr(name);
				bool ret = Global_setAnnotFont(cn);
				free(cn);
				return ret;
			}
			/// <summary>
			/// Set the transparence property of annotations
			/// </summary>
			/// <param name="color">A color containing the transparence information</param>
			static void SetAnnotTransparence(unsigned int color)
			{
				Global_setAnnotTransparency(color);
			}
			/// <summary>
			/// Get Radaee core lib version
			/// </summary>
			/// <returns></returns>
			static String^ GetVersion()
			{
				return Global_getVersion();
			}
			/// <summary>
			/// Activate Radaee lib with specified serial
			/// </summary>
			/// <param name="serial"></param>
			/// <returns>
			/// License type activated, possible values are:
			/// <0: failed to activate Radaee lib
			/// 1: standard license actived.
			/// 2: professional license actived.
			/// 3. premium license actived.</returns>
			static int ActiveLicense(String^ serial)
			{
				return Global_active(serial);
			}
			/// <summary>
			/// Get current zoom level
			/// </summary>
			static property float ZoomLevel
			{
				float get() { return zoom_level; }
				void set(float level) { zoom_level = level; }
			}
			/// <summary>
			/// Draw icon to a Bitmap object
			/// </summary>
			/// <param name="atype">1(text note) or 17(file attachment)</param>
			/// <param name="icon">Index of the icon in icon type list</param>
			/// <param name="bmp">Image resource of the icon</param>
			/// <returns>True if success, otherwise false</returns>
			static bool drawAnnotIcon(int atype, int icon, WriteableBitmap^ bmp)
			{
				return Global_drawAnnotIcon(atype, icon, bmp);
			}
			/// <summary>
			/// Draw a dash line with specified image resource
			/// </summary>
			/// <param name="dash">Dash line data</param>
			/// <param name="bmp">Image resource which will be used to draw the dash line</param>
			static void drawDashLine(const Array<float>^ dash, WriteableBitmap^ bmp)
			{
				if (dash && dash->Length > 0)
					Global_drawDashLine(dash->Data, dash->Length, bmp);
				else
					Global_drawDashLine(NULL, 0, bmp);
			}
			/// <summary>
			/// Draw a bitmap image as start/end point of line annotations
			/// </summary>
			/// <param name="head"></param>
			/// <param name="bmp"></param>
			static void drawLineHead(int head, WriteableBitmap^ bmp)
			{
				Global_drawLineHead(head, bmp);
			}
		private:
			static float zoom_level;
		};

		public ref class RDDIB sealed
		{
		public:
			RDDIB(int w, int h)
			{
				m_dib = Global_dibGet(NULL, w, h);
				//SoftwareBitmapSource^ source = ref new SoftwareBitmapSource();
				//source->SetBitmapAsync(m_bmp);
			}
			/// <summary>
			/// Resize current RDDIB object
			/// </summary>
			/// <param name="w">New width of the RDDIB</param>
			/// <param name="h">New height of the RDDIB</param>
			void Resize(int w, int h)
			{
				m_dib = Global_dibGet(m_dib, w, h);
			}
			/// <summary>
			/// Save RDDIB object to a local JPEG file
			/// </summary>
			/// <param name="path">Path of output file</param>
			/// <param name="quality">Render quality of output file</param>
			/// <returns>True if sccessed, otherwise false</returns>
			Boolean SaveJPG(String^ path, int quality)
			{
				const wchar_t* wtxt = path->Data();
				char tmp[512];
				::WideCharToMultiByte(CP_ACP, 0, wtxt, -1, tmp, 512, NULL, NULL);
				return Global_dibSaveJPG(m_dib, tmp, quality);
			}
			/// <summary>
			/// Reset the RDDIB content with specified color
			/// </summary>
			/// <param name="color">Color to reset the RDDIB</param>
			void Reset(unsigned int color)
			{
				int w = Global_dibGetWidth(m_dib);
				int h = Global_dibGetHeight(m_dib);
				unsigned int* dat_cur = (unsigned int*)Global_dibGetData(m_dib);
				unsigned int* dat_end = dat_cur + (w * h);
				while (dat_cur < dat_end) *dat_cur++ = color;
			}
			/// <summary>
			/// Draw content from another RDDIB object
			/// </summary>
			/// <param name="src">The RDDIB object holding the content</param>
			/// <param name="x">x coordinate of the position to draw</param>
			/// <param name="y">y coordinate of the position to draw</param>
			void DrawDIB(RDDIB^ src, int x, int y)
			{
				int sx = 0;
				int sy = 0;
				int dx = x;
				int dy = y;
				int dw = Global_dibGetWidth(m_dib);
				int dh = Global_dibGetHeight(m_dib);
				int sw = Global_dibGetWidth(src->m_dib);
				int sh = Global_dibGetHeight(src->m_dib);
				if (dx < 0)
				{
					sx -= dx;
					dx = 0;
				}
				if (dy < 0)
				{
					sy -= dy;
					dy = 0;
				}
				int w = dw - dx;
				int h = dh - dy;
				int w1 = sw - sx;
				int h1 = sh - sy;
				if (w < w1) w = w1;
				if (h < h1) h = h1;

				BYTE* psrc = (BYTE*)Global_dibGetData(src->m_dib) + ((sy * sw + sx) << 2);
				BYTE* pdst = (BYTE*)Global_dibGetData(m_dib) + ((dy * dw + dx) << 2);
				while (h > 0)
				{
					cpy_clr((unsigned int*)pdst, (const unsigned int*)psrc, w);
					pdst += (dw << 2);
					psrc += (sw << 2);
					h--;
				}
			}
			/// <summary>
			/// Get width of the RDDIB
			/// </summary>
			property int Width
			{
				int get() { return Global_dibGetWidth(m_dib); }
			}
			/// <summary>
			/// Get height of the RDDIB
			/// </summary>
			property int Height
			{
				int get() { return Global_dibGetHeight(m_dib); }
			}
			/// <summary>
			/// Get content data of the RDDIB
			/// </summary>
			property Array<BYTE>^ Data
			{
				Array<BYTE>^ get()
				{
					int w = Global_dibGetWidth(m_dib);
					int h = Global_dibGetHeight(m_dib);
					BYTE* dat = (BYTE*)Global_dibGetData(m_dib);
					return ArrayReference<BYTE>(dat, w * h * 4);
				}
			}
		private:
			~RDDIB()
			{
				Global_dibFree(m_dib);
				m_dib = NULL;
			}
			ID2D1Bitmap1* genDXBmp(ID2D1DeviceContext* ctx)
			{
				D2D1_SIZE_U size;
				size.width = Global_dibGetWidth(m_dib);
				size.height = Global_dibGetHeight(m_dib);
				BYTE* dat = (BYTE*)Global_dibGetData(m_dib);
				D2D1_BITMAP_PROPERTIES1 bitmapProperties = D2D1::BitmapProperties1(D2D1_BITMAP_OPTIONS_NONE,
					D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_PREMULTIPLIED),
					96, 96);
				ID2D1Bitmap1* bmp = NULL;
				ctx->CreateBitmap(size, dat, size.width << 2, &bitmapProperties, &bmp);
				return bmp;
			}
			static inline void cpy_clr(unsigned int* dst, const unsigned int* src, int count)
			{
				unsigned int* dst_end = dst + count;
				while (dst < dst_end) *dst++ = *src++;
			}
			friend RDBmp;
			friend RDDLib::pdf::PDFPage;
			friend RDDLib::pdf::PDFAnnot;
			friend RDDLib::docx::DOCXPage;
			//friend class RDPDFLib::view::DXBlock;
			PDF_DIB m_dib;
		};

		//extern byte g_tiny_bmp[];
		public ref class RDBmp sealed
		{
		public:
			RDBmp(int w, int h)
			{
				m_w = w;
				m_h = h;
				m_dib = ref new WriteableBitmap(w, h);
				m_bmp = Global_lockBitmap(m_dib);
			}
			/// <summary>
			/// Get width of the RDBmp
			/// </summary>
			property int Width
			{
				int get() { return m_w; }
			}
			/// <summary>
			/// Get height of the RDBmp
			/// </summary>
			property int Height
			{
				int get() { return m_h; }
			}
			/// <summary>
			/// Get the content data of the RDBmp as WriteableBitmap
			/// </summary>
			property WriteableBitmap^ Data
			{
				WriteableBitmap^ get()
				{
					return m_dib;
				}
			}
			/// <summary>
			/// Reset the RDBmp content with specified color
			/// </summary>
			/// <param name="color">Color to reset the RDBmp</param>
			void Reset(unsigned int color)
			{
				Global_eraseColor(m_bmp, color);
			}
			/// <summary>
			/// Save RDBmp object to a local JPEG file
			/// </summary>
			/// <param name="path">Path of output file</param>
			/// <param name="quality">Render quality of output file</param>
			/// <returns>True if sccessed, otherwise false</returns>
			Boolean SaveJPG(String^ path, int quality)
			{
				const wchar_t* wtxt = path->Data();
				char tmp[512];
				::WideCharToMultiByte(CP_ACP, 0, wtxt, -1, tmp, 512, NULL, NULL);
				return Global_saveBitmapJPG(m_bmp, tmp, quality);
			}
			/// <summary>
			/// Detach the content to a WriteableBitmap object
			/// </summary>
			/// <returns>The WriteableBitmap object</returns>
			WriteableBitmap^ Detach()
			{
				if (m_bmp)
				{
					Global_unlockBitmap(m_bmp);
					m_bmp = NULL;
					WriteableBitmap^ ret = m_dib;
					m_dib = nullptr;
					return ret;
				}
				else return nullptr;
			}
			/// <summary>
			/// Draw content from a RDDIB object to PDFBmp
			/// </summary>
			/// <param name="src">RDDIB holding the content</param>
			/// <param name="x">x coordinate of the position to draw</param>
			/// <param name="y">y coordinate of the position to draw</param>
			void DrawDIB(RDDIB^ src, int x, int y)
			{
				if (src) Global_drawDIB(m_bmp, src->m_dib, x, y);
			}
			virtual ~RDBmp()
			{
				if (m_bmp)
				{
					Global_unlockBitmap(m_bmp);
					m_bmp = NULL;
					m_dib = nullptr;
				}
			}
		internal:
			friend RDDLib::pdf::PDFPage;
			friend RDDLib::pdf::PDFAnnot;
			WriteableBitmap^ m_dib;
			PDF_BMP m_bmp;
			int m_w;
			int m_h;
		};
		public interface class IRDStream
		{
		public:
			/// <summary>
			/// Get if the stream writeable
			/// </summary>
			/// <returns>True or false</returns>
			bool Writeable();
			/// <summary>
			/// Get available data length
			/// </summary>
			/// <returns>Length of the data</returns>
			long long GetLength();
			/// <summary>
			/// Get current position in the stream
			/// </summary>
			/// <returns>Index of the position</returns>
			long long GetPosition();
			/// <summary>
			/// Set and jump to specified postion in the stream
			/// </summary>
			/// <param name="pos">Position in the stream</param>
			/// <returns>True if successed, otherwise false</returns>
			bool SetPosition(long long pos);
			/// <summary>
			/// Read data from stream into buffer
			/// </summary>
			/// <param name="buf">buffer to receive the data</param>
			/// <returns>Length of bytes read</returns>
			int Read(WriteOnlyArray<BYTE>^ buf);
			/// <summary>
			/// Write data into stream from buffer
			/// </summary>
			/// <param name="buf">Buffer holding the data</param>
			/// <returns>Length of bytes written</returns>
			int Write(const Array<BYTE>^ buf);
			/// <summary>
			/// Close the stream
			/// </summary>
			void Close();
			/// <summary>
			/// Flush changes into stream
			/// </summary>
			void Flush();
		};
		public ref class RDSoftBmp sealed
		{
		public:
			RDSoftBmp(int w, int h)
			{
				m_w = w;
				m_h = h;
				m_dib = ref new SoftwareBitmap(BitmapPixelFormat::Bgra8, w, h, BitmapAlphaMode::Premultiplied);
				m_bmp = Global_lockSoftBitmap(m_dib);
			}
			/// <summary>
			/// Get width of the bitmap
			/// </summary>
			property int Width
			{
				int get() { return m_w; }
			}
			/// <summary>
			/// Get height of the bitmap
			/// </summary>
			property int Height
			{
				int get() { return m_h; }
			}
			/// <summary>
			/// Get bitmap of the bitmap as SoftwareBitmap
			/// </summary>
			property SoftwareBitmap^ Data
			{
				SoftwareBitmap^ get()
				{
					return m_dib;
				}
			}
			/// <summary>
			/// Reset the PDFSoftBmp content with specified color
			/// </summary>
			/// <param name="color">Color to reset the PDFSoftBmp</param>
			void Reset(unsigned int color)
			{
				Global_eraseColor(m_bmp, color);
			}
			/// <summary>
			/// Save PDFSoftBmp object to a local JPEG file
			/// </summary>
			/// <param name="path">Path of output file</param>
			/// <param name="quality">Render quality of output file</param>
			/// <returns>True if sccessed, otherwise false</returns>
			Boolean SaveJPG(String^ path, int quality)
			{
				const wchar_t* wtxt = path->Data();
				char tmp[512];
				::WideCharToMultiByte(CP_ACP, 0, wtxt, -1, tmp, 512, NULL, NULL);
				return Global_saveBitmapJPG(m_bmp, tmp, quality);
			}
		private:
			~RDSoftBmp()
			{
				Global_unlockBitmap(m_bmp);
				m_dib = nullptr;
			}
			friend RDDLib::pdf::PDFPage;
			friend RDDLib::pdf::PDFAnnot;
			SoftwareBitmap^ m_dib;
			PDF_BMP m_bmp;
			int m_w;
			int m_h;
		};
		public ref class RDPath sealed
		{
		public:
			RDPath()
			{
				m_path = Path_create();
			}
			/// <summary>
			/// Perform a move to operation, update current postion
			/// </summary>
			/// <param name="x">x coordinate of the postion to move to</param>
			/// <param name="y">y coordinate of the postion to move to</param>
			void MoveTo(float x, float y)
			{
				Path_moveTo(m_path, x, y);
			}
			/// <summary>
			/// Perform a line to operation from current postiion to specified postion
			/// </summary>
			/// <param name="x">x coordinate of the end postion of the line</param>
			/// <param name="y">y coordinate of the end postion of the line</param>
			void LineTo(float x, float y)
			{
				Path_lineTo(m_path, x, y);
			}
			/// <summary>
			/// Draw a curve from start postiion to specified postion
			/// </summary>
			/// <param name="x1">x coordinate of the start postion of the curve</param>
			/// <param name="y1">y coordinate of the start postion of the curve</param>
			/// <param name="x2">x coordinate of the end postion of the curve</param>
			/// <param name="y2">y coordinate of the end postion of the curve</param>
			/// <param name="x3">x coordinate of the peak postion of the curve</param>
			/// <param name="y4">y coordinate of the peak postion of the curve</param>
			void CurveTo(float x1, float y1, float x2, float y2, float x3, float y3)
			{
				Path_curveTo(m_path, x1, y1, x2, y2, x3, y3);
			}
			/// <summary>
			/// Perform a line to operation from current position to the start position of the path to close it.
			/// </summary>
			void Close()
			{
				Path_closePath(m_path);
			}
			/// <summary>
			/// Get node count of the path
			/// </summary>
			property int NodesCnt
			{
				int get() { return Path_getNodeCount(m_path); }
			}
			/// <summary>
			/// Get the operation on specified node
			/// </summary>
			/// <param name="index">Index of node</param>
			/// <returns>Operation on the node</returns>
			int GetOP(int index)
			{
				PDF_POINT pt;
				return Path_getNode(m_path, index, &pt);
			}
			/// <summary>
			/// Get specified point
			/// </summary>
			/// <param name="index">Index of the point</param>
			/// <returns>A PDFPoint object</returns>
			RDPoint GetPoint(int index)
			{
				PDF_POINT pt;
				Path_getNode(m_path, index, &pt);
				return *(RDPoint*)&pt;
			}
		private:
			RDPath(PDF_PATH path)
			{
				m_path = path;
			}
			friend ref class RDMatrix;
			friend pdf::PDFPageContent;
			friend pdf::PDFPage;
			friend pdf::PDFAnnot;
			~RDPath()
			{
				Path_destroy(m_path);
			}
			PDF_PATH m_path;
		};
		public ref class RDInk sealed
		{
		public:
			RDInk(float width, unsigned int color)
			{
				m_ink = Ink_create(width, color);
			}
			/// <summary>
			/// Trigger a down event for ink
			/// </summary>
			/// <param name="x">x coordinate of the postion of down event</param>
			/// <param name="y">y coordinate of the postion of down event</param>
			void Down(float x, float y)
			{
				Ink_onDown(m_ink, x, y);
			}
			/// <summary>
			/// Trigger a move event for ink
			/// </summary>
			/// <param name="x">x coordinate of the postion of move event</param>
			/// <param name="y">y coordinate of the postion of move event</param>
			void Move(float x, float y)
			{
				Ink_onMove(m_ink, x, y);
			}
			/// <summary>
			/// Trigger a up event for ink
			/// </summary>
			/// <param name="x">x coordinate of the postion of up event</param>
			/// <param name="y">y coordinate of the postion of up event</param>
			void Up(float x, float y)
			{
				Ink_onUp(m_ink, x, y);
			}
			/// <summary>
			/// Get node count in the ink
			/// </summary>
			property int NodesCnt
			{
				int get() { return Ink_getNodeCount(m_ink); }
			}
			/// <summary>
			/// Get operation on specified node
			/// </summary>
			/// <param name="index">Index of node</param>
			/// <returns>The operation</returns>
			int GetOP(int index)
			{
				PDF_POINT pt;
				return Ink_getNode(m_ink, index, &pt);
			}
			/// <summary>
			/// Get point object at with index
			/// </summary>
			/// <param name="index">Index of the point</param>
			/// <returns>A PDFPoint object</returns>
			RDPoint GetPoint(int index)
			{
				PDF_POINT pt;
				Ink_getNode(m_ink, index, &pt);
				return *(RDPoint*)&pt;
			}
		private:
			friend ref class RDMatrix;
			friend pdf::PDFPage;
			~RDInk()
			{
				Ink_destroy(m_ink);
			}
			PDF_INK m_ink;
		};
		public ref class RDMatrix sealed
		{
		public:
			RDMatrix(float scalex, float scaley, float x0, float y0)
			{
				m_mat = Matrix_createScale(scalex, scaley, x0, y0);
			}
			RDMatrix(float xx, float yx, float xy, float yy, float x0, float y0)
			{
				m_mat = Matrix_create(xx, yx, xy, yy, x0, y0);
			}
			void Invert()
			{
				Matrix_invert(m_mat);
			}
			/// <summary>
			/// Transform a PDFPath object
			/// </summary>
			/// <param name="path">The PDFPath object to transform</param>
			void TransformPath(RDPath^ path)
			{
				Matrix_transformPath(m_mat, path->m_path);
			}
			/// <summary>
			/// Transform a PDFInk object
			/// </summary>
			/// <param name="path">The PDFInk object to transform</param>
			void TransformInk(RDInk^ ink)
			{
				Matrix_transformInk(m_mat, ink->m_ink);
			}
			/// <summary>
			/// Transform a rectangle
			/// </summary>
			/// <param name="path">The PDFRect object to transform</param>
			RDRect TransformRect(RDRect rect)
			{
				Matrix_transformRect(m_mat, (PDF_RECT*)&rect);
				return rect;
			}
			/// <summary>
			/// Transform a point
			/// </summary>
			/// <param name="path">The PDFPoint object to transform</param>
			RDPoint TransformPoint(RDPoint point)
			{
				Matrix_transformPoint(m_mat, (PDF_POINT*)&point);
				return point;
			}
		private:
			RDMatrix()
			{
				m_mat = NULL;
			}
			friend pdf::PDFPage;
			friend pdf::PDFPageContent;
			~RDMatrix()
			{
				Matrix_destroy(m_mat);
			}
			PDF_MATRIX m_mat;
		};
	}
}

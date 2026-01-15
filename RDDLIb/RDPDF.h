#pragma once

#include "UWPPDF.h"
#include "RDCom.h"
#include <stdlib.h>
#include <windows.h>

using namespace Platform;
using namespace Windows::Graphics::Imaging;
using namespace RDDLib::comm;
char *cvt_str_cstr( String ^str );
String ^cvt_cstr_str( const char *str );

namespace RDDLib
{
	namespace pdf
	{
		ref class PDFOutline;
		ref class PDFPage;
		ref class PDFAnnot;
		ref class PDFDocImage;
		ref class PDFDocFont;
		ref class PDFDocForm;
		ref class PDFDocGState;
		ref class PDFSign;
		ref class PDFImportCtx;
		ref class PDFPageContent;
		ref class PDFResForm;
		ref class PDFResFont;
		ref class PDFResImage;
		ref class PDFResGState;
		ref class PDFDoc;
		/**
		* value to record object reference.
		*/
		public value struct PDFRef
		{
			unsigned long long ref;
		};
		/**
		* value to record text layout results.
		*/
		public value struct PDFTextRet
		{
			/**
			* number of unicodes that drawn on PDF content.
			*/
			int num_unicodes;
			/**
			* number of lines that drawn on PDF content.
			*/
			int num_lines;
		};
		public interface class PDFJSDelegate
		{
		public:
			/// <summary>
			/// Console command
			/// </summary>
			/// <param name="cmd">
			/// *0: clear console.
			/// *1: hide console.
			///	*2: print line on sonsole.
			///	*3: show console.</param>
			/// <param name="para">Only valid when cmd == 2;</param>
			void OnConsole(int cmd, String ^para);
			/// <summary>
			/// Show a alert dialog on screen.
			/// </summary>
			/// <param name="btn">uttons show on dialog.
			/// *0: OK
			///	*1: OK, Cancel
			///	*2: Yes, No
			///	*3: Yes, No, Cancel</param>
			/// <param name="msg">Message to be show.</param>
			/// <param name="title">Title to be show.</param>
			/// <returns>Tbutton user clicked. Values as below:
			/// *1: OK
			///	*2: Cancel
			///	*3: No
			///	*4: Yes</returns>
			int OnAlert(int btn, String ^msg, String ^title);
			/// <summary>
			/// Callback when document closed.
			/// </summary>
			/// <returns>True is the Document object need save, otherwise false.</returns>
			bool OnDocClose();
			/// <summary>
			/// Generate a tmp file name that JS needed in background.
			/// </summary>
			/// <returns>Absolute path to temp path generated.</returns>
			String^ OnTmpFile();
			/// <summary>
			/// Callback when an Uncaught exception appears.
			/// </summary>
			/// <param name="code">Error code.</param>
			/// <param name="msg">Error message.</param>
			void OnUncaughtException(int code, String^ msg);
		};
		public ref class PDFDocTag sealed
		{
		internal:
			PDFDocTag(PDF_TAG tag)
			{
				m_tag = tag;
			}
		private:
			friend PDFDoc;
			friend PDFPage;
			PDF_TAG m_tag;
		};
		public ref class PDFObj sealed
		{
		public:
			PDFObj()
			{
				m_obj = PDF_Obj_create();
				m_ref = false;
			}
			property int type
			{
				int get(){ return PDF_Obj_getType(m_obj); }
			}
			property int IntVal
			{
				int get(){ return PDF_Obj_getInt(m_obj); }
				void set(int v){ PDF_Obj_setInt(m_obj, v); }
			}
			property float RealVal
			{
				float get(){ return PDF_Obj_getReal(m_obj); }
				void set(float v){ PDF_Obj_setReal(m_obj, v); }
			}
			property bool BoolVal
			{
				bool get(){ return PDF_Obj_getBoolean(m_obj); }
				void set(bool v){ PDF_Obj_setBoolean(m_obj, v); }
			}
			property String ^NameVal
			{
				String ^get()
				{
					const char *cname = PDF_Obj_getName(m_obj);
					if (!cname) return nullptr;
					int clen = strlen(cname);
					wchar_t *wsname = (wchar_t *)malloc(sizeof(wchar_t) * (clen + 1));
					MultiByteToWideChar(CP_ACP, 0, cname, -1, wsname, clen + 1);
					String ^ret = ref new String(wsname);
					free(wsname);
					return ret;
				}
				void set(String ^name)
				{
					const wchar_t *wsname = name->Data();
					int wlen = name->Length();
					char *cname = (char *)malloc(wlen * 4 + 4);
					WideCharToMultiByte(CP_ACP, 0, wsname, -1, cname, wlen * 4 + 4, NULL, NULL);
					PDF_Obj_setName(m_obj, cname);
					free(cname);
				}
			}
			property String ^AsciiStringVal
			{
				String ^get()
				{
					const char *cname = PDF_Obj_getAsciiString(m_obj);
					if (!cname) return nullptr;
					int clen = strlen(cname);
					wchar_t *wsname = (wchar_t *)malloc(sizeof(wchar_t) * (clen + 1));
					MultiByteToWideChar(CP_ACP, 0, cname, -1, wsname, clen + 1);
					String ^ret = ref new String(wsname);
					free(wsname);
					return ret;
				}
				void set(String ^name)
				{
					const wchar_t *wsname = name->Data();
					int wlen = name->Length();
					char *cname = (char *)malloc(wlen * 4 + 4);
					WideCharToMultiByte(CP_ACP, 0, wsname, -1, cname, wlen * 4 + 4, NULL, NULL);
					PDF_Obj_setAsciiString(m_obj, cname);
					free(cname);
				}
			}
			property String ^TextStringVal
			{
				String ^get()
				{
					return PDF_Obj_getTextString(m_obj);
				}
				void set(String ^name)
				{
					PDF_Obj_setTextString(m_obj, name->Data());
				}
			}
			property Array<BYTE> ^HexStringVal
			{
				Array<BYTE> ^get()
				{
					int len;
					unsigned char *data = PDF_Obj_getHexString(m_obj, &len);
					if (!data) return nullptr;
					return ArrayReference<BYTE>((BYTE *)data, len);
				}
				void set(const Array<BYTE> ^v)
				{
					BYTE *data = v->Data;
					int len = v->Length;
					PDF_Obj_setHexString(m_obj, data, len);
				}
			}
			property PDFRef RefVal
			{
				PDFRef get()
				{
					PDFRef ref;
					ref.ref = PDF_Obj_getReference(m_obj);
					return ref;
				}
				void set(PDFRef ref)
				{
					PDF_Obj_setReference(m_obj, ref.ref);
				}
			}
			/// <summary>
			/// Set a PDF_OBJ as dictionary
			/// </summary>
			void SetDictionary()
			{
				PDF_Obj_dictGetItemCount(m_obj);
			}
			/// <summary>
			/// Get dictionary item count
			/// </summary>
			/// <returns>Item count</returns>
			int DictGetItemCount()
			{
				return PDF_Obj_dictGetItemCount(m_obj);
			}
			/// <summary>
			/// Get tag of a dictionary item
			/// </summary>
			/// <param name="index">Index of the item</param>
			/// <returns>The tag content</returns>
			String ^DictGetItemTag(int index)
			{
				const char *tag = PDF_Obj_dictGetItemName(m_obj, index);
				if (!tag) return nullptr;
				int clen = strlen(tag);
				wchar_t *wsname = (wchar_t *)malloc(sizeof(wchar_t) * (clen + 1));
				MultiByteToWideChar(CP_ACP, 0, tag, -1, wsname, clen + 1);
				String ^ret = ref new String(wsname);
				free(wsname);
				return ret;
			}
			/// <summary>
			/// Get a dictionary item with index
			/// </summary>
			/// <param name="index">Index of the item</param>
			/// <returns>A PDFObj object which is the item</returns>
			PDFObj ^DictGetItem(int index)
			{
				PDF_OBJ obj = PDF_Obj_dictGetItemByIndex(m_obj, index);
				if (!obj) return nullptr;
				PDFObj ^ret = ref new PDFObj(obj);
				return ret;
			}
			/// <summary>
			/// Get a dictionary item with tag
			/// </summary>
			/// <param name="tag">Tag of the item</param>
			/// <returns>A PDFObj object which is the item</returns>
			PDFObj ^DictGetItem(String ^tag)
			{
				const wchar_t *wsname = tag->Data();
				int wlen = tag->Length();
				char *cname = (char *)malloc(wlen * 4 + 4);
				WideCharToMultiByte(CP_ACP, 0, wsname, -1, cname, wlen * 4 + 4, NULL, NULL);
				PDF_OBJ obj = PDF_Obj_dictGetItemByName(m_obj, cname);
				free(cname);
				if (!obj) return nullptr;
				PDFObj ^ret = ref new PDFObj(obj);
				return ret;
			}
			/// <summary>
			/// Set a dictionary item with tag
			/// </summary>
			/// <param name="tag">Tag of the item</param>
			void DictSetItem(String ^tag)
			{
				const wchar_t *wsname = tag->Data();
				int wlen = tag->Length();
				char *cname = (char *)malloc(wlen * 4 + 4);
				WideCharToMultiByte(CP_ACP, 0, wsname, -1, cname, wlen * 4 + 4, NULL, NULL);
				PDF_Obj_dictSetItem(m_obj, cname);
				free(cname);
			}
			/// <summary>
			/// Remove a dictionary item with tag
			/// </summary>
			/// <param name="tag">Tag of the item</param>
			void DictRemoveItem(String ^tag)
			{
				const wchar_t *wsname = tag->Data();
				int wlen = tag->Length();
				char *cname = (char *)malloc(wlen * 4 + 4);
				WideCharToMultiByte(CP_ACP, 0, wsname, -1, cname, wlen * 4 + 4, NULL, NULL);
				PDF_Obj_dictRemoveItem(m_obj, cname);
				free(cname);
			}
			/// <summary>
			/// Set a PDF_OBJ object as an array
			/// </summary>
			void SetArray()
			{
				PDF_Obj_arrayClear(m_obj);
			}
			/// <summary>
			/// Get item count of an array
			/// </summary>
			/// <returns>The number of item count</returns>
			int ArrayGetItemCount()
			{
				return PDF_Obj_arrayGetItemCount(m_obj);
			}
			/// <summary>
			/// Get an item from the array with index
			/// </summary>
			/// <param name="index">Index of the item</param>
			/// <returns>The item with the index if found</returns>
			PDFObj ^ArrayGetItem(int index)
			{
				PDF_OBJ obj = PDF_Obj_arrayGetItem(m_obj, index);
				if (!obj) return nullptr;
				PDFObj ^ret = ref new PDFObj(obj);
				return ret;
			}
			/// <summary>
			/// Append a new item to existing array
			/// </summary>
			void ArrayAppendItem()
			{
				PDF_Obj_arrayAppendItem(m_obj);
			}
			/// <summary>
			/// Insert a new item to existing array with specified position
			/// </summary>
			/// <param name="index">The position to insert the item</param>
			void ArrayInsertItem(int index)
			{
				PDF_Obj_arrayInsertItem(m_obj, index);
			}
			/// <summary>
			/// Remove an item from existing array with specified position
			/// </summary>
			/// <param name="index">The position of the item to remove</param>
			void ArrayRemoveItem(int index)
			{
				PDF_Obj_arrayRemoveItem(m_obj, index);
			}
			/// <summary>
			/// Clear all elements in existing array
			/// </summary>
			void ArrayClear()
			{
				PDF_Obj_arrayClear(m_obj);
			}
		private:
			PDFObj(PDF_OBJ obj)
			{
				m_obj = obj;
				m_ref = true;
			}
			~PDFObj()
			{
				if (!m_ref)
				{
					PDF_Obj_destroy(m_obj);
				}
			}
			friend ref class PDFDoc;
			friend ref class PDFAnnot;
			PDF_OBJ m_obj;
			bool m_ref;
		};
		/**
		* class for HTML exporting.
		*/
		public ref class PDFHtmlExporter sealed
		{
		internal:
			PDFHtmlExporter(PDF_DOC doc, PDF_HTML_EXPORTER exp)
			{
				m_doc = doc;
				m_exp = exp;
			}
			/**
			* export page to destination file.
			*/
			bool ExportPage(int pageno)
			{
				return PDF_Document_htmExpPage(m_doc, m_exp, pageno);
			}
			/**
			* finish exporting and release all related resources.
			*/
			void ExportEnd(bool binary)
			{
				if (binary) PDF_Document_htmExpBinEnd(m_doc, m_exp);
				else PDF_Document_htmExpEnd(m_doc, m_exp);
				m_exp = NULL;
			}
		private:
			~PDFHtmlExporter()
			{
				if(m_exp) PDF_Document_htmExpEnd(m_doc, m_exp);
			}
			PDF_DOC m_doc;
			PDF_HTML_EXPORTER m_exp;
		};
		public ref class PDFDoc sealed
		{
		public:
			PDFDoc();
			static void SetOpenFlag(int flag)
			{
				PDF_Document_setOpenFlag(flag);
			}
			/**
			 * open document.<br/>
			 * first time, SDK try password as user password, and then try password as owner password.
			 * @param stream IRandomAccessStream object.
			 * @param password password or null.
			 * @return error code:<br/>
			 * 0:succeeded, and continue<br/>
			 * -1:need input password<br/>
			 * -2:unknown encryption<br/>
			 * -3:damaged or invalid format<br/>
			 * -10:access denied or invalid file path<br/>
			 * others:unknown error
			 */
			RD_ERROR Open( IRandomAccessStream ^stream, String ^password );
			/**
			 * open document from customer stream.
			 * first time, SDK try password as user password, and then try password as owner password.
			 * @param stream IRDStream object.
			 * @param password password or null.
			 * @return error code:<br/>
			 * 0:succeeded, and continue<br/>
			 * -1:need input password<br/>
			 * -2:unknown encryption<br/>
			 * -3:damaged or invalid format<br/>
			 * -10:access denied or invalid file path<br/>
			 * others:unknown error
			 */
			RD_ERROR OpenStream(IRDStream^stream, String ^password );
			/**
			 * open document.<br/>
			 * first time, SDK try password as user password, and then try password as owner password.
			 * @param path PDF file to be open.
			 * @param password password or null.
			 * @return error code:<br/>
			 * 0:succeeded, and continue<br/>
			 * -1:need input password<br/>
			 * -2:unknown encryption<br/>
			 * -3:damaged or invalid format<br/>
			 * -10:access denied or invalid file path<br/>
			 * others:unknown error
			 */
			RD_ERROR OpenPath( String ^path, String ^password );
			/**
			 * create a empty PDF document
			 * @param stream IRandomAccessStream object that is writable.
			 * @return 0 or less than 0 means failed, same as Open.
			 */
			RD_ERROR Create( IRandomAccessStream ^stream )
			{
				PDF_ERR err;
				m_doc = PDF_Document_create( stream, &err );
				return (RD_ERROR)err;
			}
			/**
			 * create a empty PDF document
			 * @param stream stream to create
			 * @return 0 or less than 0 means failed, same as Open.
			 */
			RD_ERROR CreateStream(IRDStream^stream )
			{
				PDF_ERR err;
				m_inner = new PDFStreamInner;
				m_inner->Open( stream );
				m_doc = PDF_Document_createForStream( m_inner, &err );
				return (RD_ERROR)err;
			}
			/**
			 * create a empty PDF document
			 * @param path path to create
			 * @return 0 or less than 0 means failed, same as Open.
			 */
			RD_ERROR CreatePath( String ^path )
			{
				PDF_ERR err;
				char *cpath = cvt_str_cstr( path );
				m_doc = PDF_Document_createForPath( cpath, &err );
				free( cpath );
				return (RD_ERROR)err;
			}
			/**
			 * linearizied status.
			 * 0: linearized header not loaded or no linearized header.(if setOpenFlag(0); cause always return 0)<br/>
			 * 1: there is linearized header, but linearized entry checked as failed.<br/>
			 * 2: there is linearized header, linearized entry checked succeeded, but hint table is damaged.<br/>
			 * 3. linearized header loaded succeeded.
			 */
			property int LinearizedStatus
			{
				int get() { return PDF_Document_getLinearizedStatus(m_doc); }
			}
			/**
			 * set cache file to PDF.<br/>
			 * a professional or premium license is required for this method.
			 * @param path a path to save some temporary data, compressed images and so on
			 * @return true or false
			 */
			void SetCahce( String ^path )
			{
				char *cpath = cvt_str_cstr( path );
				PDF_Document_setCache( m_doc, cpath );
				free( cpath );
			}
			/**
			 * run javascript, NOTICE:considering some complex js, this method is not thread-safe.<br/>
			 * this method require premium license, it always return false if using other license type.
			 * @param js javascript string, can't be null.
			 * @param del delegate for javascript running, can't be null.
			 * @return if js or del is null, or no premium license actived, return false.<br/>
			 * if success running, return true.<br/>
			 * otherwise, an exception shall throw to java.
			 */
			bool RunJS(String ^js, PDFJSDelegate ^del)
			{
				PDFJSDelegateInner idel(del);
				const wchar_t *wstmp = js->Data();
				int len = wcslen(wstmp) + 1;
				char *stmp = (char *)malloc(sizeof(wchar_t) * len);
				::WideCharToMultiByte(CP_ACP, 0, wstmp, -1, stmp, len * sizeof(wchar_t), NULL, NULL);
				bool ret = PDF_Document_runJS(m_doc, stmp, &idel);
				free(stmp);
				return ret;
			}
			/**
			 * save the document.<br/>
			 * this always return false, if no license actived.
			 * @return true or false
			 */
			Boolean Save();
			/**
			 * save as the document to another file.<br/>
			 * @param path path to save.
			 * @return true or false.
			 */
			Boolean SaveAs(String ^path)
			{
				if (!path) return false;
				return PDF_Document_saveAsW(m_doc, path->Data());
			}
			/**
			 * save to file using optimize flags
			 * @param path dst path to save.
			 * @param opts 10 elements set flags, it can pass null, if using default flag.<br/>
			 * opts[0]: 1-convert transparency group to image, this may make poor quality.
			 * opts[1]: convert flag for colored images, 0-non, 1-to jpg, 2-to jpx(reserved), 3-to zlib.
			 * opts[2]: convert flag for grayed images, 0-non, 1-to zlib compressed
			 * opts[3]: convert for mask and mono images, 0-non, 1-to zlib, 2-to jbig2
			 * opts[4]: remove unsed objects?(ignored, set to 0 or 1 has same effect)
			 * opts[5]: remove tags?(ignored, set to 0 or 1 has same effect)
			 * opts[6]: remove thumbnail for each page?
			 * opts[7]: merge little piece of images in same area to an big image?(not support yet)
			 * opts[8]: generate linearized header? this mean web optimizing.
			 * opts[9]: generate pdfa?(current only pdfa header generated.)
			 * @param img_dpi if dpi of image in page is greater than this value, image will be resized.
			 * @return true or false
			 */
			Boolean SaveAs(String^ path, const Array<unsigned char>^ opts, float img_dpi)
			{
				if (!path || !opts || opts->Length < 10) return false;
				return PDF_Document_optimizeAsW(m_doc, path->Data(), opts->Data, img_dpi);
			}
			/**
			 * encrypt document and save as the document to another file.<br/>
			 * this method require premium license.
			 * @param dst path to save�� same as path parameter of SaveAs.
			 * @param User_pswd user password, can be null.
			 * @param Owner_pswd owner password, can be null.
			 * @param perm permission to set, same as GetPermission() method.<br/>
			 * bit 1-2 reserved<br/>
			 * bit 3(0x4) print<br/>
			 * bit 4(0x8) modify<br/>
			 * bit 5(0x10) extract text or image<br/>
			 * others: see PDF reference
			 * @param method set 3 means using AES 256bits encrypt(Acrobat X), V=5 and R = 6 mode, others AES with V=4 and R=4 mode.
			 * @param id must be 32 bytes for file ID. it is divided to 2 array in native library, as each 16 bytes.
			 * @return true or false.
			 */
			Boolean EncryptAs(String^ dst, String ^User_pswd, String ^Owner_pswd, int perm, int method, const Array<BYTE> ^fid)
			{
				if (!dst || !fid || fid->Length < 32) return false;
				const wchar_t* ws_path = dst->Data();
				const wchar_t* ws_upswd = User_pswd->Data();
				const wchar_t* ws_opswd = Owner_pswd->Data();
				const BYTE* pb_fid = fid->Data;
				return PDF_Document_encryptAsW(m_doc, ws_path, ws_upswd, ws_opswd, perm, method, pb_fid);
			}
			void Close();
			/**
			 * max width and max height of all pages.
			 */
			property RDPoint MaxPageSize
			{
				RDPoint get()
				{
					PDF_POINT pt = PDF_Document_getPagesMaxSize(m_doc);
					return *(RDPoint *)&pt;
				}
			}
			/**
			 * get page width by page NO.
			 * @param pageno 0 based page NO. range:[0, GetPageCount()-1]
			 * @return width value.
			 */
			float GetPageWidth(int pageno);
			/**
			 * get page height by page NO.
			 * @param pageno 0 based page NO. range:[0, GetPageCount()-1]
			 * @return height value.
			 */
			float GetPageHeight(int pageno);
			/**
			 * get label of page
			 * @param pageno 0 based page index number
			 * @return json string or pure text. for json: name is style name of number.<br/>
			 * for example:<br/>
			 * {"D":2} is "2"<br/>
			 * {"R":3} is "III"<br/>
			 * {"r":4} is "iv"<br/>
			 * {"A":5} is "E"<br/>
			 * {"a":6} is "f"<br/>
			 * for pure text: the text is the label.
			 */
			String ^GetPageLabel(int pageno)
			{
				wchar_t wtxt[512];
				if (!PDF_Document_getPageLabel(m_doc, pageno, wtxt, 511)) return nullptr;
				return ref new String(wtxt);
			}
			/**
			 * set page rotate.<br/>
			 * @param pageno 0 based page NO.
			 * @param degree rotate angle in degree, must be 90 * n.
			 * @return true or false
			 */
			bool SetPageRotate(int pageno, int degree)
			{
				return PDF_Document_setPageRotate(m_doc, pageno, degree);
			}
			/**
			 * get meta data of document.
			 * @param tag Predefined values:"Title", "Author", "Subject", "Keywords", "Creator", "Producer", "CreationDate", "ModDate".<br/>or you can pass any key that self-defined.
			 * @return Meta string value, or null.
			 */
			String ^GetMeta(String ^tag);
			/**
			 * set meta data for document.<br/>
			 * this method valid only in premium version.
			 * @param tag Predefined values:"Title", "Author", "Subject", "Keywords", "Creator", "Producer", "CreationDate", "ModDate".<br/>or you can pass any key that self-defined.
			 * @param val string value.
			 * @return true or false.
			 */
			bool SetMeta(String^ tag, String^ val);
			/**
			 * export form data as xml string.<br/>
			 * this method require premium license.
			 * @return xml string or null.
			 */
			String ^ExportForm();
			/// <summary>
			/// export XFDF format from PDF document.
			/// </summary>
			/// <param name="href">reference to source PDF file path.</param>
			/// <returns>XFDF format string</returns>
			String^ ExportXFDF(String^ href);
			/// <summary>
			/// import XFDF to PDF document.
			/// </summary>
			/// <param name="xfdf">XFDF format string, a xml string include encoding header.</param>
			/// <returns>true or false.</returns>
			bool ImportXFDF(String^ xfdf);
			PDFOutline ^GetRootOutline();
			Boolean AddRootOutline( String ^label, int dest, float y );
			/**
			 * get a Page object for page NO.
			 * @param pageno 0 based page NO. range:[0, GetPageCount()-1]
			 * @return Page object or null.
			 */
			PDFPage ^GetPage(int pageno);
			/**
			 * create an image from Bitmap object.<br/>
			 * @param bmp Bitmap object in ARGB_8888/ARGB_4444/RGB_565 format.
			 * @param has_alpha generate alpha channel information?
			 * @return DocImage object or null.
			 */
			PDFDocImage ^NewImage(WriteableBitmap ^bitmap, bool has_alpha, bool interpolate);
			PDFDocImage^ NewImage(SoftwareBitmap^ bitmap, bool has_alpha, bool interpolate);
			PDFDocImage^ NewImage(WriteableBitmap^ bitmap, unsigned int matte, bool interpolate);
			PDFDocImage^ NewImage(SoftwareBitmap^ bitmap, unsigned int matte, bool interpolate);
			/**
			 * create an image from JPEG/JPG file.<br/>
			 * supported image color space:<br/>
			 * --GRAY<br/>
			 * --RGB<br/>
			 * --CMYK<br/>
			 * @param path path to JPEG file.
			 * @return DocImage object or null.
			 */
			PDFDocImage ^NewImageJPEG(String ^path, bool interpolate);
			/**
			 * create an image from JPX/JPEG 2k file.<br/>
			 * @param path path to JPX file.
			 * @return DocImage object or null.
			 */
			PDFDocImage ^NewImageJPX(String ^path, bool interpolate);
			/**
			 * create a font object, used to write texts.<br/>
			 * @param name <br/>
			 * font name exists in font list.<br/>
			 * using Global.getFaceCount(), Global.getFaceName() to enumerate fonts.
			 * @param style <br/>
			 *   (style&1) means bold,<br/>
			 *   (style&2) means Italic,<br/>
			 *   (style&8) means embed,<br/>
			 *   (style&16) means vertical writing, mostly used in Asia fonts.
			 * @return DocFont object or null is failed.
			 */
			PDFDocFont ^NewFontCID( String ^name, int style );
			/**
			 * create a ExtGraphicState object, used to set alpha values.<br/>
			 * @return DocGState object or null.
			 */
			PDFDocGState ^NewGState();
			/**
			 * new a form from Document level.
			 * @return DocForm object or null.
			 */
			PDFDocForm	^NewForm();
			/**
			 * insert a page to Document<br/>
			 * if pagheno >= page_count, it do same as append.<br/>
			 * otherwise, insert to pageno.<br/>
			 * @param pageno 0 based page NO.
			 * @param w page width in PDF coordinate
			 * @param h page height in PDF coordinate
			 * @return Page object or null means failed.
			 */
			PDFPage ^NewPage( int pageno, float w, float h );
			/**
			 * remove page by page NO.<br/>
			 * @param pageno 0 based page NO.
			 * @return true or false
			 */
			Boolean RemovePage( int pageno );
			/**
			 * move the page to other position.<br/>
			 * @param srcno page NO, move from
			 * @param dstno page NO, move to
			 * @return true or false
			 */
			Boolean MovePage( int srcno, int dstno );
			/**
			 * Start import operations, import page from src<br/>
			 * you shall maintenance the source Document object until all pages are imported and ImportContext.Destroy() invoked.
			 * @param src source Document object that opened.
			 * @return a context object used in ImportPage.
			 */
			PDFImportCtx ^ImportStart(PDFDoc ^src);
			property unsigned long long Handler
			{
				unsigned long long get() { return (unsigned long long)m_doc; }
			}
			/**
			* pages count.
			*/
			property int PageCount
			{
				int get() {return PDF_Document_getPageCount(m_doc);}
			}
			/**
			 * permission of PDF, this value defined in PDF reference 1.7<br/>
			 * mostly, it means the permission from encryption.<br/>
			 * bit 1-2 reserved<br/>
			 * bit 3(0x4) print<br/>
			 * bit 4(0x8) modify<br/>
			 * bit 5(0x10) extract text or image<br/>
			 * others: see PDF reference
			 */
			property int Permission
			{
				int get() {return PDF_Document_getPermission( m_doc );}
			}
			property String^ XMP
			{
				String^ get() { return PDF_Document_getXMP(m_doc); }
				void set(String^ xmp) { PDF_Document_setXMP(m_doc, xmp); }
			}
			/**
			 * permission of PDF, this value defined in "Perm" entry in Catalog object.<br/>
			 * mostly, it means the permission from signature.<br/>
			 * 0 means not defined<br/>
			 * 1 means can't modify<br/>
			 * 2 means can modify some form fields<br/>
			 * 3 means can do any modify<br/>
			 */
			property int Perm
			{
				int get() {return PDF_Document_getPerm( m_doc );}
			}
			/**
			 * embed files count, for document level.<br/>
			 */
			property int EFCount
			{
				int get() { return PDF_Document_getEFCount(m_doc); }
			}
			/**
			 * get Description of embed file.
			 * @param index range in [0, GetEmbedFilesCount())
			 * @return Description of embed file
			 */
			String^ GetEFDesc(int index)
			{
				return PDF_Document_getEFDesc(m_doc, index);
			}
			/**
			 * get name of embed file.
			 * @param index range in [0, GetEmbedFilesCount())
			 * @return name of embed file
			 */
			String^ GetEFName(int index)
			{
				return PDF_Document_getEFName(m_doc, index);
			}
			/**
			 * get embed file data, and save to save_path
			 * @param index range in [0, GetEmbedFilesCount())
			 * @param path absolute path to save embed file.
			 * @return true or false.
			 */
			bool GetEFData(int index, String ^path)
			{
				return PDF_Document_getEFData(m_doc, index, path);
			}
			/**
			 * delete embed file.
			 * @param index range in [0, GetEmbedFilesCount())
			 * @return true or false.
			 */
			bool DelEFData(int index)
			{
				return PDF_Document_delEF(m_doc, index);
			}
			/**
			 * new embed file.
			 * @param path absolute path to the file to be embed.
			 * @return true or false.
			 */
			bool NewEF(String ^path)
			{
				return PDF_Document_newEF(m_doc, path);
			}
			/**
			 * java script count, for document level.<br/>
			 */
			property int JSCount
			{
				int get() { return PDF_Document_getJSCount(m_doc); }
			}
			/**
			 * get javascript.
			 * @param index range in [0, GetJSCount())
			 * @return javascript string
			 */
			String^ GetJS(int index)
			{
				return PDF_Document_getJS(m_doc, index);
			}
			/**
			 * get name of javascript.
			 * @param index range in [0, GetJSCount())g
			 * @return name of javascript
			 */
			String^ GetJSName(int index)
			{
				return PDF_Document_getJSName(m_doc, index);
			}
			/**
			 * create group tag.<br/>
			 * NOTICE:"Document" is auto created by native library.
			 * @param parent parent group. if it is null, the created tag is top level tag.
			 * @param tag predefined tag name, defined in PDF-Reference-1.7, chapter 10.7.3, TABLE 10.20, except "Document" tag.
			 * @return group tag object relate to Document object, and the block tag relate to page object.
			 */
			PDFDocTag^ NewTagGroup(PDFDocTag^ parent, String^ stag)
			{
				PDF_TAG ret = NULL;
				if (parent) ret = PDF_Document_newTagGroup(m_doc, parent->m_tag, stag);
				else ret = PDF_Document_newTagGroup(m_doc, NULL, stag);
				if (!ret) return nullptr;
				else return ref new PDFDocTag(ret);
			}
			/**
			 * check if document can be modified or saved.<br/>
			 */
			property Boolean CanSave
			{
				Boolean get() {return PDF_Document_canSave( m_doc );}
			}
			/**
			 * check if document is encrypted.
			 */
			property Boolean IsEncrypted
			{
				Boolean get() {return PDF_Document_isEncrypted( m_doc );}
			}
			/**
			 * check if opened.
			 */
			property Boolean IsOpened
			{
				Boolean get() { return m_doc != NULL; }
			}
			/// <summary>
			/// Get PDFObj with reference
			/// </summary>
			/// <param name="ref">Reference to target PDFObj</param>
			/// <returns>A PDFObj object if found</returns>
			PDFObj ^Advance_GetObj(PDFRef ref)
			{
				PDF_OBJ obj = PDF_Document_advGetObj(m_doc, ref.ref);
				if (!obj) return nullptr;
				PDFObj ^ret = ref new PDFObj(obj);
				return ret;
			}
			/// <summary>
			/// Get reference of current PDFObj
			/// </summary>
			/// <returns>Reference of current PDFObj</returns>
			PDFRef Advance_GetRef()
			{
				PDF_OBJ_REF ref = PDF_Document_advGetRef(m_doc);
				PDFRef ret;
				ret.ref = ref;
				return ret;
			}
			/// <summary>
			/// Advanced function to create an empty indirect object to edit. This method require premium license.
			/// </summary>
			/// <returns>Reference to created object</returns>
			PDFRef Advance_NewIndirect()
			{
				PDF_OBJ_REF ref = PDF_Document_advNewIndirectObj(m_doc);
				PDFRef ret;
				ret.ref = ref;
				return ret;
			}
			/// <summary>
			/// Advanced function to create an indirect object, and then copy source object to this indirect object. This method require premium license.
			/// </summary>
			/// <param name="obj">Source object to be copied</param>
			/// <returns>Reference to created object</returns>
			PDFRef Advance_NewIndirectAndCopy(PDFObj ^obj)
			{
				PDF_OBJ_REF ref = PDF_Document_advNewIndirectObjWithData(m_doc, obj->m_obj);
				PDFRef ret;
				ret.ref = ref;
				return ret;
			}
			/// <summary>
			/// Advanced function to reload document objects.
			/// </summary>
			void Advance_Reload()
			{
				PDF_Document_advReload(m_doc);
			}
			/// <summary>
			/// Advanced function to create a stream using zflate compression(zlib).
			/// Stream byte contents can't modified, once created.
			/// The byte contents shall auto compress and encrypt by native library.
			/// This method must be invoked after invoking Document.SetCache().
			/// </summary>
			/// <param name="src">The source data to create the stream</param>
			/// <returns>Reference to created object</returns>
			PDFRef Advance_NewFlateStream(const Array<BYTE> ^src)
			{
				int len = src->Length;
				const unsigned char *data = src->Data;
				PDF_OBJ_REF ref = PDF_Document_advNewFlateStream(m_doc, data, len);
				PDFRef ret;
				ret.ref = ref;
				return ret;
			}
			PDFRef Advance_NewRawStream(const Array<BYTE> ^src)
			{
				int len = src->Length;
				const unsigned char *data = src->Data;
				PDF_OBJ_REF ref = PDF_Document_advNewRawStream(m_doc, data, len);
				PDFRef ret;
				ret.ref = ref;
				return ret;
			}
			/**
			 * verify the signature<br/>
			 * @param sign signature object from PDFAnnot.GetSign()
			 * @return 0 if verify OK, others are error.
			 */
			int VerifySign(PDFSign ^sign);
			PDFHtmlExporter^ HtmlExportStart(String ^path)
			{
				PDF_HTML_EXPORTER exp = PDF_Document_htmExpStart(m_doc, path);
				if (!exp) return nullptr;
				return ref new PDFHtmlExporter(m_doc, exp);
			}
		private:
			class PDFStreamInner:public IPDFStream
			{
			public:
				void Open(IRDStream^stream )
				{
					m_stream = stream;
				}
				virtual bool Writeable() const
				{
					return m_stream->Writeable();
				}
				virtual unsigned long long GetLen() const
				{
					return m_stream->GetLength();
				}
				virtual unsigned long long GetPos() const
				{
					return m_stream->GetPosition();
				}
				virtual bool SetPos( unsigned long long off )
				{
					return m_stream->SetPosition(off);
				}
				virtual unsigned int Read( void *pBuf, unsigned int dwBuf )
				{
					ArrayReference<BYTE> tmp((BYTE*)pBuf, dwBuf);
					return m_stream->Read( tmp );
				}
				virtual unsigned int Write( const void *pBuf, unsigned int dwBuf )
				{
					ArrayReference<BYTE> tmp((BYTE*)pBuf, dwBuf);
					return m_stream->Write( tmp );
				}
				virtual void Close()
				{
					m_stream->Close();
					m_stream = nullptr;
				}
				virtual void Flush()
				{
					m_stream->Flush();
				}
			protected:
				IRDStream^m_stream;
			};
			class PDFJSDelegateInner : public IPDFJSDelegate
			{
			public:
				PDFJSDelegateInner(PDFJSDelegate ^del)
				{
					m_del = del;
				}
				virtual void OnConsole(int cmd, const char *para)
				{
					int max = strlen(para) + 1;
					wchar_t *wstmp = (wchar_t *)malloc(sizeof(wchar_t) * max);
					::MultiByteToWideChar(CP_ACP, 0, para, -1, wstmp, max);
					String ^tmp = ref new String(wstmp);
					free(wstmp);
					m_del->OnConsole(cmd, tmp);
				}
				virtual int OnAlert(int btn, const char *msg, const char *title)
				{
					int max_msg = strlen(msg) + 1;
					wchar_t *wsmsg = (wchar_t *)malloc(sizeof(wchar_t) * max_msg);
					::MultiByteToWideChar(CP_ACP, 0, msg, -1, wsmsg, max_msg);
					String ^tmp_msg = ref new String(wsmsg);
					free(wsmsg);

					int max_title = strlen(title) + 1;
					wchar_t *wstitle = (wchar_t *)malloc(sizeof(wchar_t) * max_title);
					::MultiByteToWideChar(CP_ACP, 0, title, -1, wstitle, max_title);
					String ^tmp_title = ref new String(wstitle);
					free(wstitle);
					return m_del->OnAlert(btn, tmp_msg, tmp_title);
				}
				virtual bool OnDocClose()
				{
					return m_del->OnDocClose();
				}
				virtual char *OnTmpFile()
				{
					String ^tmp = m_del->OnTmpFile();
					const wchar_t *wstmp = tmp->Data();
					int len = wcslen(wstmp) + 1;
					char *stmp = (char *)malloc(len * sizeof(wchar_t));
					::WideCharToMultiByte(CP_ACP, 0, wstmp, -1, stmp, len * sizeof(wchar_t), NULL, NULL);
					return stmp;
				}
				virtual void OnUncaughtException(int code, const char *msg)
				{
					int max = strlen(msg) + 1;
					wchar_t *wstmp = (wchar_t *)malloc(sizeof(wchar_t) * max);
					::MultiByteToWideChar(CP_ACP, 0, msg, -1, wstmp, max);
					String ^tmp = ref new String(wstmp);
					free(wstmp);
					m_del->OnUncaughtException(code, tmp);
				}
			private:
				PDFJSDelegate ^m_del;
			};
			friend PDFDocFont;
			friend PDFDocGState;
			friend PDFDocForm;
			friend PDFImportCtx;
			friend PDFOutline;
			friend docx::DOCXDoc;
			~PDFDoc();
			PDF_DOC m_doc;
			PDFStreamInner *m_inner;
		};
		public ref class PDFSign sealed
		{
		public:
			property String ^Issue
			{
				String ^get()
				{
					return PDF_Sign_getIssue(m_sign);
				}
			}
			property String ^Subject
			{
				String ^get()
				{
					return PDF_Sign_getSubject(m_sign);
				}
			}
			property long long Version
			{
				long long get()
				{
					return PDF_Sign_getVersion(m_sign);
				}
			}
			/**
			 * get signer name.
			 */
			property String^ Name
			{
				String^ get()
				{
					return PDF_Sign_getName(m_sign);
				}
			}
			/**
			 * sign reason
			 */
			property String^ Reason
			{
				String^ get()
				{
					return PDF_Sign_getReason(m_sign);
				}
			}
			/**
			 * sign location.
			 */
			property String^ Location
			{
				String^ get()
				{
					return PDF_Sign_getLocation(m_sign);
				}
			}
			/**
			 * sign contact string
			 */
			property String^ Contact
			{
				String^ get()
				{
					return PDF_Sign_getContact(m_sign);
				}
			}
			/**
			 * sign date time
			 */
			property String^ ModTime
			{
				String^ get()
				{
					return PDF_Sign_getModDT(m_sign);
				}
			}
			property Array<BYTE>^ Content
			{
				Array<BYTE>^ get()
				{
					return PDF_Sign_getContent(m_sign);
				}
			}
		private:
			PDFSign()
			{
				m_sign = NULL;
			}
			friend PDFDoc;
			friend PDFPage;
			friend PDFDocForm;
			friend PDFAnnot;
			PDF_SIGN m_sign;
		};
		/**
		* class to record handle value of image object.
		*/
		public ref class PDFDocImage sealed
		{
		public:
		private:
			PDFDocImage()
			{
				m_image = NULL;
			}
			friend PDFDoc;
			friend PDFPage;
			friend PDFDocForm;
			PDF_DOC_IMAGE m_image;
		};
		/**
		* class to record handle value of font object.
		*/
		public ref class PDFDocFont sealed
		{
		public:
			/**
			 * get ascent
			 * ascent based in 1, for example: 0.88f
			 */
			property float Ascent
			{
				float get(){return PDF_Document_getFontAscent(m_doc->m_doc, m_font);}
			}
			/**
			 * get descent
			 * ascent based in 1, for example: -0.12f
			 */
			property float Descent
			{
				float get(){return PDF_Document_getFontDescent(m_doc->m_doc, m_font);}
			}
		private:
			PDFDocFont()
			{
				m_font = NULL;
			}
			friend PDFDoc;
			friend PDFPage;
			friend PDFDocForm;
			friend PDFAnnot;
			friend PDFPageContent;
			PDFDoc ^m_doc;
			PDF_DOC_FONT m_font;
		};
		/**
		* class to record handle value of graphic status object.
		*/
		public ref class PDFDocGState sealed
		{
		public:
			/**
			 * set alpha value for fill and other non-stroke operation
			 * @param alpha range: [0, 255]
			 * @return true or false
			 */
			void SetFillAlpha(int alpha)
			{
				PDF_Document_setGStateFillAlpha(m_doc->m_doc, m_gs, alpha);
			}
			/**
			 * set alpha value for stroke operation
			 * @param alpha range: [0, 255]
			 * @return true or false
			 */
			void SetStrokeAlpha(int alpha)
			{
				PDF_Document_setGStateStrokeAlpha(m_doc->m_doc, m_gs, alpha);
			}
			/**
			 * set dash for stroke operation.
			 * @param dash dash arra, if null, means set to solid.
			 * @param phase phase value, mostly, it is 0.
			 * @return true or false.
			 * eaxmple:<br/>
			 * [2, 1], 0  means 2 on, 1 off, 2 on, 1 off, ��<br/>
			 * [2, 1], 0.5 means 1.5 on, 1 off, 2 on 1 off, ��<br/>
			 * for more details, plz see PDF-Reference 1.7 (4.3.2) Line Dash Pattern.<br/>
			 */
			void SetStrokeDash(const Array<float> ^dash, float phase)
			{
				if(dash)
					PDF_Document_setGStateStrokeDash(m_doc->m_doc, m_gs, dash->Data, dash->Length, phase);
				else
					PDF_Document_setGStateStrokeDash(m_doc->m_doc, m_gs, NULL, 0, 0);
			}
			/**
			 * set blend mode to graphic state.
			 * @param bmode 2:Multipy<br/>
			 *              3:Screen<br/>
			 *              4:Overlay<br/>
			 *              5:Darken<br/>
			 *              6:Lighten<br/>
			 *              7:ColorDodge<br/>
			 *              8:ColorBurn<br/>
			 *              9:Difference<br/>
			 *              10:Exclusion<br/>
			 *              11:Hue<br/>
			 *              12:Saturation<br/>
			 *              13:Color<br/>
			 *              14:Luminosity<br/>
			 *              others:Normal
			 * @return true or false.
			 */
			void SetBlendMode(int bmode)
			{
				PDF_Document_setGStateBlendMode(m_doc->m_doc, m_gs, bmode);
			}
		private:
			PDFDocGState()
			{
				m_gs = NULL;
			}
			friend PDFDoc;
			friend PDFPage;
			friend PDFDocForm;
			PDF_DOC_GSTATE m_gs;
			PDFDoc ^m_doc;
		};
		public ref class PDFDocForm sealed
		{
		public:
			/**
			 * add sub-form as resource of form.<br/>
			 * @param dform returned by PDFDoc.NewForm()
			 * @return resource handle
			 */
			PDFResForm ^AddResForm(PDFDocForm ^sub);
			/**
			 * add font as resource of form.
			 * @param dfont returned by PDFDoc.NewFontCID()
			 * @return resource handle
			 */
			PDFResFont ^AddResFont(PDFDocFont ^font);
			/**
			 * add Graphic State as resource of form.
			 * @param dgs returned by PDFDoc.NewGState()
			 * @return resource handle
			 */
			PDFResGState ^AddResGState(PDFDocGState ^gs);
			/**
			 * add image as resource of form.
			 * @param dimg returned by PDFDoc.NewImageXXX()
			 * @return resource handle
			 */
			PDFResImage ^AddResImage(PDFDocImage ^img);
			/**
			 * set content of form, need a box defined in form.<br/>
			 * the box define edge of form area, which PDFPageContent object includes.
			 * @param content PDFPageContent object.
			 * @param x x of form's box
			 * @param y y of form's box
			 * @param w width of form's box
			 * @param h height of form's box
			 */
			void SetContent(PDFPageContent ^content, float x, float y, float w, float h);
			/**
			 * set this form as transparency.
			 * @param isolate set to isolate, mostly are false.
			 * @param knockout set to knockout, mostly are false.
			 */
			void SetTransparency(bool isolate, bool knockout)
			{
				PDF_Document_setFormTransparency(m_doc->m_doc, m_form, isolate, knockout);
			}
		private:
			PDFDocForm()
			{
				m_form = NULL;
			}
			~PDFDocForm()
			{
				PDF_Document_freeForm(m_doc->m_doc, m_form);
			}
			friend PDFDoc;
			friend PDFPage;
			friend PDFAnnot;
			PDFDoc ^m_doc;
			PDF_DOC_FORM m_form;
		};
		/**
		* Context handle for importing.
		*/
		public ref class PDFImportCtx sealed
		{
		public:
			/// <summary>
			/// import a page to the document
			/// Do not forget to invoke ImportContext.Destroy() after all pages are imported.
			/// </summary>
			/// <param name="srcno">0 based page NO.from source Document that passed to ImportStart.</param>
			/// <param name="dstno">0 based page NO.to insert in this document object.</param>
			/// <returns>True if successed, otherwise false</returns>
			Boolean ImportPage(int srcno, int dstno)
			{
				return PDF_Document_importPage(m_doc->m_doc, m_ctx, srcno, dstno);
			}
			/**
			 * import a page to another page.<br/>
			 * do not forget to invoke ImportContext.Destroy() after all pages are imported.
			 * @param srcno 0 based page NO. from source Document that passed to ImportStart.
			 * @param dstno 0 based page NO. to insert in this document object.
			 * @rect rectangle in destno Page object, that srcno Page will display on.
			 * @return true or false.
			 */
			Boolean ImportPage2Page(int srcno, int dstno, RDRect rect)
			{
				return PDF_Document_importPage2Page(m_doc->m_doc, m_ctx, srcno, dstno, (const PDF_RECT *)&rect);
			}
		private:
			PDFImportCtx()
			{
				m_ctx = NULL;
				m_doc = nullptr;
			}
			~PDFImportCtx()
			{
				PDF_Document_importEnd( m_doc->m_doc, m_ctx );
			}
			friend PDFDoc;
			friend PDFPage;
			PDFDoc ^m_doc;
			PDF_IMPORTCTX m_ctx;
		};
		/**
		* Outline node class.
		*/
		public ref class PDFOutline sealed
		{
		public:
			/**
			* get next outline node.
			*/
			PDFOutline ^GetNext();
			/**
			* get first child outline node.
			*/
			PDFOutline ^GetChild();
			/**
			* insert outline after of this Outline.
			*/
			Boolean AddNext( String ^label, int dest, float y );
			/**
			* insert outline as first child of this Outline.
			*/
			Boolean AddChild( String ^label, int dest, float y );
			/**
			* remove this Outline, and all children of this Outline.
			*/
			Boolean RemoveFromDoc();
			/**
			 * label of Outline
			 */
			property String^label
			{
				String ^get()
				{
					return PDF_Document_getOutlineLabel( m_doc->m_doc, m_outline);
				}
			}
			/**
			 * destination jumping page NO of Outline.
			 */
			property int dest
			{
				int get()
				{
					return PDF_Document_getOutlineDest( m_doc->m_doc, m_outline );
				}
			}
			property Array<int>^ dest_para
			{
				Array<int>^ get()
				{
					Array<int>^ tmp = ref new Array<int>(7);
					PDF_Document_getOutlineDest2(m_doc->m_doc, m_outline, tmp->Data);
					return tmp;
				}
			}
		private:
			PDFOutline()
			{
				m_doc = nullptr;
				m_outline = NULL;
			}
			friend PDFDoc;
			PDFDoc ^m_doc;
			PDF_OUTLINE m_outline;
		};
		public ref class PDFPageTag sealed
		{
		internal:
			PDFPageTag(PDF_TAG tag)
			{
				m_tag = tag;
			}
		private:
			friend PDFPage;
			friend PDFPageContent;
			PDF_TAG m_tag;
		};
		public ref class PDFResGState sealed
		{
		public:
		private:
			PDFResGState()
			{
				m_gs = NULL;
			}
			friend PDFDocForm;
			friend PDFPage;
			friend PDFPageContent;
			PDF_PAGE_GSTATE m_gs;
		};
		public ref class PDFResImage sealed
		{
		public:
		private:
			PDFResImage()
			{
				m_image = NULL;
			}
			friend PDFDocForm;
			friend PDFPage;
			friend PDFPageContent;
			PDF_PAGE_IMAGE m_image;
		};
		public ref class PDFResFont sealed
		{
		public:
		private:
			PDFResFont()
			{
				m_font = NULL;
			}
			friend PDFDocForm;
			friend PDFPage;
			friend PDFPageContent;
			PDF_PAGE_FONT m_font;
		};
		public ref class PDFResForm sealed
		{
		public:
		private:
			PDFResForm()
			{
				m_form = NULL;
			}
			friend PDFDocForm;
			friend PDFPage;
			friend PDFPageContent;
			PDF_PAGE_FORM m_form;
		};
		public ref class PDFPageContent sealed
		{
		public:
			PDFPageContent()
			{
				m_content = PDF_PageContent_create();
			}
			/**
			 * PDF operator: gs_save, save current GraphicState
			 */
			void GSSave()
			{
				PDF_PageContent_gsSave( m_content );
			}
			/**
			 * PDF operator: gs_restore, restore GraphicState
			 */
			void GSRestore()
			{
				PDF_PageContent_gsRestore( m_content );
			}
			/**
			 * PDF operator: set ExtGraphicState
			 * @param gs PDFResGState object created by PDFPage.AddResGState() or PDFDocForm.AddResGState()
			 */
			void GSSet( PDFResGState ^gs )
			{
				PDF_PageContent_gsSet( m_content, gs->m_gs );
			}
			/**
			 * PDF operator: set matrix.(concat matrix operator)
			 * @param mat Matrix object
			 */
			void GSSetMatrix(RDMatrix^mat )
			{
				PDF_PageContent_gsSetMatrix( m_content, mat->m_mat );
			}
			/**
			 * PDF operator: begin text and set text position to (0,0).
			 */
			void TextBegin()
			{
				PDF_PageContent_textBegin( m_content );
			}
			/**
			 * PDF operator: text end.
			 */
			void TextEnd()
			{
				PDF_PageContent_textEnd( m_content );
			}
			/**
			 * PDF operator: show image.
			 * @param image image object created by PDFPage.AddResImage() or PDFDocForm.AddResImage()
			 */
			void DrawImage( PDFResImage ^img )
			{
				PDF_PageContent_drawImage( m_content, img->m_image );
			}
			/**
			 * PDF operator: show form.
			 * @param frm Form object created by PDFPage.AddResForm() or PDFDocForm.AddResForm()
			 */
			void DrawForm(PDFResForm ^form)
			{
				PDF_PageContent_drawForm(m_content, form->m_form);
			}
			/**
			 * show text
			 * @param text text to show, '\r' or '\n' in string start a new line.
			 */
			void DrawText(String ^text)
			{
				PDF_PageContent_drawTextW( m_content, text->Data() );
			}
			/**
			 * show text with special width
			 * @param text text to show.
			 * @param align 0:left, 1: middle, 2:right
			 * @param width bounding width to draw text
			 * @return line count of this text drawing.
			 */
			int DrawText(String^ text, int align, float width)
			{
				return PDF_PageContent_drawText2W(m_content, text->Data(), align, width);
			}
			/**
			 * show text with special width
			 * @param text text to show.
			 * @param align 0:left, 1: middle, 2:right
			 * @param width bounding width to draw text
			 * @param max_lines max line count will be drawn
			 * @return PDFTextRet value struct.
			 */
			PDFTextRet DrawText(String^ text, int align, float width, int max_lines)
			{
				int val = PDF_PageContent_drawText3W(m_content, text->Data(), align, width, max_lines);
				PDFTextRet ret;
				ret.num_unicodes = val & ((1 << 20) - 1);
				ret.num_lines = val >> 20;
				return ret;
			}
			RDPoint GetTextSize(String ^text, PDFResFont ^pfont, float width, float height, float char_space, float word_space)
			{
				PDF_POINT pt = PDF_PageContent_textGetSizeW(m_content, pfont->m_font, text->Data(), width, height, char_space, word_space);
				return *(RDPoint *)&pt;
			}
			/**
			 * stroke path.
			 * @param path RDPath object
			 */
			void StrokePath( RDPath ^path )
			{
				PDF_PageContent_strokePath( m_content, path->m_path );
			}
			/**
			 * stroke path.
			 * @param path RDPath object
			 * @param winding winding fill rules?
			 */
			void FillPath( RDPath ^path, Boolean winding )
			{
				PDF_PageContent_fillPath( m_content, path->m_path, winding );
			}
			/**
			 * set the path as clip path.
			 * @param path RDPath object
			 * @param winding winding fill rule?
			 */
			void ClipPath( RDPath ^path, Boolean winding )
			{
				PDF_PageContent_clipPath( m_content, path->m_path, winding );
			}
			/**
			 * PDF operator: set fill and other operations color.
			 * @param color formatted as 0xRRGGBB, no alpha channel. alpha value shall set by ExtGraphicState(ResGState).
			 */
			void SetFillColor( unsigned int color )
			{
				PDF_PageContent_setFillColor(m_content, color);
			}
			/**
			 * PDF operator: set stroke color.
			 * @param color formatted as 0xRRGGBB, no alpha channel. alpha value shall set by ExtGraphicState(ResGState).
			 */
			void SetStrokeColor(unsigned int color )
			{
				PDF_PageContent_setStrokeColor(m_content, color);
			}
			/**
			 * PDF operator: set line cap
			 * @param cap 0:butt, 1:round: 2:square
			 */
			void SetStrokeCap( int cap )
			{
				PDF_PageContent_setStrokeCap(m_content, cap);
			}
			/**
			 * PDF operator: set line join
			 * @param join 0:miter, 1:round, 2:bevel
			 */
			void SetStrokeJoin( int join )
			{
				PDF_PageContent_setStrokeJoin(m_content, join);
			}
			/**
			 * PDF operator: set line width
			 * @param w line width in PDF coordinate
			 */
			void SetStrokeWidth( float w )
			{
				PDF_PageContent_setStrokeWidth(m_content, w);
			}
			/**
			 * PDF operator: set miter limit.
			 * @param miter miter limit.
			 */
			void SetStrokeMiter( float miter )
			{
				PDF_PageContent_setStrokeMiter(m_content, miter);
			}
			void SetStrokeDash(const Array<float>^ dash, float phase)
			{
				if (dash && dash->Length > 0)
					PDF_PageContent_setStrokeDash(m_content, dash->Data, dash->Length, phase);
				else
					PDF_PageContent_setStrokeDash(m_content, NULL, 0, phase);
			}
			/**
			 * PDF operator: set char space(extra space between chars).
			 * @param space char space
			 */
			void TextSetCharSpace( float space )
			{
				PDF_PageContent_textSetCharSpace( m_content, space );
			}
			/**
			 * PDF operator: set word space(extra space between words spit by blank char ' ' ).
			 * @param space word space.
			 */
			void TextSetWordSpace( float space )
			{
				PDF_PageContent_textSetWordSpace( m_content, space );
			}
			/**
			 * PDF operator: set text leading, height between 2 text lines.
			 * @param leading leading in PDF coordinate
			 */
			void TextSetLeading( float leading )
			{
				PDF_PageContent_textSetLeading( m_content, leading );
			}
			/**
			 * PDF operator: set text rise
			 * @param rise
			 */
			void TextSetRise( float rise )
			{
				PDF_PageContent_textSetRise( m_content, rise );
			}
			/**
			 * PDF operator: set horizon scale for chars.
			 * @param scale 100 means scale value 1.0f
			 */
			void TextSetHScale( int scale )
			{
				PDF_PageContent_textSetHScale( m_content, scale );
			}
			/**
			 * PDF operator: new a text line
			 */
			void TextNextLine()
			{
				PDF_PageContent_textNextLine( m_content );
			}
			/**
			 * PDF operator: move text position relative to previous line
			 * @param x in PDF coordinate add to previous line position
			 * @param y in PDF coordinate add to previous line position
			 */
			void TextMove( float x, float y )
			{
				PDF_PageContent_textMove( m_content, x, y );
			}
			/**
			 * set text font
			 * @param font ResFont object created by PDFPage.AddResFont() or PDFDocForm.AddResFont()
			 * @param size text size in PDF coordinate.
			 */
			void TextSetFont( PDFResFont ^font, float size )
			{
				PDF_PageContent_textSetFont( m_content, font->m_font, size );
			}
			/**
			 * PDF operator: set text render mode.
			 * @param mode <br/>
			 * 0: filling<br/>
			 * 1: stroke<br/>
			 * 2: fill and stroke<br/>
			 * 3: do nothing<br/>
			 * 4: fill and set clip path<br/>
			 * 5: stroke and set clip path<br/>
			 * 6: fill/stroke/clip<br/>
			 * 7: set clip path.
			 */
			void TextSetRenderMode( int mode )
			{
				PDF_PageContent_textSetRenderMode( m_content, mode );
			}
			/**
			 * start a tag, this function shall pair with TagEnd().
			 * @param blk block tag return from Page.NewTagBlock();
			 */
			void TagStart(PDFPageTag^ tag)
			{
				PDF_PageContent_tagBlockStart(m_content, tag->m_tag);
			}
			/**
			 * end a tag, this function shall pair with TagBlockStart();
			 */
			void TagEnd()
			{
				PDF_PageContent_tagBlockEnd(m_content);
			}
		private:
			friend PDFPage;
			friend PDFDocForm;
			~PDFPageContent()
			{
				PDF_PageContent_destroy( m_content );
			}
			PDF_PAGECONTENT m_content;
		};
		public ref class PDFFinder sealed
		{
		public:
			/**
			 * get matched count in this page.
			 * @return count or 0 if no found.
			 */
			int GetCount()
			{
				return PDF_Page_findGetCount( m_finder );
			}
			/**
			 * get first char index.
			 * @param index 0 based index value. range:[0, FindGetCount()-1]
			 * @return the first char index of texts, see: ObjsGetString. range:[0, ObjsGetCharCount()-1]
			 */
			int GetFirstChar( int index )
			{
				return PDF_Page_findGetFirstChar( m_finder, index );
			}
			int GetLastChar(int index)
			{
				return PDF_Page_findGetEndChar(m_finder, index);
			}
		private:
			PDFFinder()
			{
				m_finder = 0;
			}
			~PDFFinder()
			{
				PDF_Page_findClose( m_finder );
			}
			friend PDFPage;
			PDF_FINDER m_finder;
		};
		public ref class PDFEditNode sealed
		{
		public:
			static void SetDefFont(String^ fname)
			{
				PDF_EditNode_setDefFont(fname);
			}
			static void SetDefCJKFont(String^ fname)
			{
				PDF_EditNode_setDefCJKFont(fname);
			}
			static bool caret_is_end(long long pos)
			{
				return (pos & 1) != 0;
			}
			static bool caret_is_vert(long long pos)
			{
				return (pos & 2) != 0;
			}
			static bool caret_is_same(long long pos0, long long pos1)
			{
				if (pos0 == pos1) return true;
				int ic0 = (int)((pos0 >> 16) & 65535);
				int ic1 = (int)((pos1 >> 16) & 65535);
				return ((pos0 >> 32) == (pos1 >> 32) && ic0 + 1 == ic1 && !caret_is_end(pos0) && caret_is_end(pos1));
			}
			static long long caret_regular_end(long long pos)
			{
				if (caret_is_end(pos))
				{
					int ic0 = ((int)((pos >> 16) & 65535)) + 1;
					int if0 = ((int)(pos & 65535)) & (~1);
					pos &= (~0xffffffffl);
					pos += (ic0 << 16) + if0;
				}
				return pos;
			}
			static bool caret_is_first(long long pos)
			{
				return ((pos >> 32) == 0 && ((pos >> 16) & 65535) == 0 && (pos & 1) == 0);
			}

			long long caret_regular_start(long long pos)
			{
				if (caret_is_end(pos))
				{
					pos &= (~1l);
					pos = GetCharNext(pos);
				}
				return pos;
			}
			long long GetCharPos(float pdfx, float pdfy)
			{
				return PDF_EditNode_getCharPos(m_node, pdfx, pdfy);
			}
			long long GetCharPrev(long long pos)
			{
				return PDF_EditNode_getCharPrev(m_node, pos);
			}
			long long GetCharNext(long long pos)
			{
				return PDF_EditNode_getCharNext(m_node, pos);
			}
			long long GetCharPrevLine(float y, long long pos)
			{
				return PDF_EditNode_getCharPrevLine(m_node, y, pos);
			}
			long long GetCharNextLine(float y, long long pos)
			{
				return PDF_EditNode_getCharNextLine(m_node, y, pos);
			}
			/**
			 * get relative rect area in Text block
			 * @param pos return from GetCharPos
			 * @return relative rect area in Text block
			 */
			RDRect GetCharRect(long long pos)
			{
				PDF_RECT rect;
				if (!PDF_EditNode_getCharRect(m_node, pos, &rect))
				{
					rect.left = 0;
					rect.right = 0;
					rect.top = 0;
					rect.bottom = 0;
				}
				return *(RDRect *)&rect;
			}
			void CharDelete(long long start, long long end)
			{
				PDF_EditNode_charDelete(m_node, start, end);
			}
			String^ CharGetString(long long start, long long end)
			{
				return PDF_EditNode_charGetString(m_node, start, end);
			}
			void CharReturn(long long pos)
			{
				PDF_EditNode_charReturn(m_node, pos);
			}
			long long CharInsert(long long pos, String^ sval)
			{
				return PDF_EditNode_charInsert(m_node, pos, sval);
			}
			long long CharHome(long long pos)
			{
				return PDF_EditNode_getCharBegLine(m_node, pos);
			}
			long long CharEnd(long long pos)
			{
				return PDF_EditNode_getCharEndLine(m_node, pos);
			}
			property int Type
			{
				int get() { return PDF_EditNode_getType(m_node); }
			}
			/**
			 * rect area in PDF coordinate
			 */
			property RDRect Rect
			{
				RDRect get()
				{
					PDF_RECT rect;
					if (!PDF_EditNode_getRect(m_node, &rect))
					{
						rect.left = 0;
						rect.right = 0;
						rect.top = 0;
						rect.bottom = 0;
					}
					return *(RDRect *)&rect;
				}
				void set(RDRect rect)
				{
					PDF_EditNode_setRect(m_node, (PDF_RECT *)&rect);
				}
			}
			/**
			 * update text block size, after text deleted/text inserted/line returned.
			 */
			void UpdateRect()
			{
				PDF_EditNode_updateRect(m_node);
			}
			void Delete()
			{
				PDF_EditNode_delete(m_node);
				m_node = NULL;
			}
		internal:
			PDFEditNode(PDF_EDITNODE node)
			{
				m_node = node;
			}
		private:
			~PDFEditNode()
			{
				PDF_EditNode_destroy(m_node);
				m_node = NULL;
			}
			PDF_EDITNODE m_node;
		};
		public ref class PDFPage sealed
		{
		public:
			/**
			 * rotated CropBox.
			 */
			property RDRect CropBox
			{
				RDRect get()
				{
					PDF_RECT rc;
					PDF_Page_getCropBox( m_page, &rc );
					return *(RDRect *)&rc;
				}
			}
			/**
			 * rotated MediaBox.
			 */
			property RDRect MediaBox
			{
				RDRect get()
				{
					PDF_RECT rc;
					PDF_Page_getMediaBox( m_page, &rc );
					return *(RDRect *)&rc;
				}
			}
			/**
			 * import annotation from memory(byte array)<br/>
			 * @param rect [left, top, right, bottom] in PDF coordinate. which is the import annotation's position.
			 * @param data data returned from Annotation.Export()
			 * @return true or false.
			 */
			bool ImportAnnot(RDRect rect, const Array<BYTE> ^buf, int buf_len)
			{
				return PDF_Page_importAnnot(m_page, (PDF_RECT *)&rect, buf->Data, buf_len);
			}

			/**
			 * Start Reflow.<br/>
			 * @param width input width, function calculate height.
			 * @param ratio scale base to 72 DPI, 2.0 means 144 DPI. the reflowed text will displayed in scale
			 * @param reflow_images enable reflow images.
			 * @return the height that reflow needed.
			 */
			float ReflowStart(float width, float ratio, bool reflow_images)
			{
				return PDF_Page_reflowStart(m_page, width, ratio, reflow_images);
			}
			/**
			 * Reflow to dib.<br/>
			 * @param dib dib to render
			 * @param orgx origin x coordinate
			 * @param orgy origin y coordinate
			 * @return true or false
			 */
			Boolean Reflow(RDDIB ^dib, float orgx, float orgy)
			{
				return PDF_Page_reflow(m_page, dib->m_dib, orgx, orgy);
			}
			/**
			 * Reflow to Bitmap object.<br/>
			 * @param bitmap bitmap to reflow
			 * @param orgx origin x coordinate
			 * @param orgy origin y coordinate
			 * @return true or false
			 */
			Boolean ReflowToBmp(RDBmp ^bmp, float orgx, float orgy)
			{
				return PDF_Page_reflowToBmp(m_page, bmp->m_bmp, orgx, orgy);
			}

			/**
			 * prepare to render. it reset dib pixels to white value, and reset page status.<br/>
			 * if dib is null, only to reset page status.
			 * @param dib DIB object to render. get from Global.dibGet() or null.
			 */
			void RenderPrepare( RDDIB ^dib )
			{
				if (dib)
					PDF_Page_renderPrepare(m_page, dib->m_dib);
				else
					PDF_Page_renderPrepare(m_page, NULL);
			}
			/**
			 * prepare to render, and reset page status.<br/>
			 */
			void RenderPrepare()
			{
				PDF_Page_renderPrepare( m_page, NULL );
			}
			/**
			 * render page to dib object. this function returned for cancelled or finished.<br/>before render, you need invoke RenderPrepare.
			 * @param dib DIB object to render.
			 * @param mat Matrix object define scale, rotate, translate operations.
			 * @param mode poor/normal/best.
			 * @return true or false.
			 */
			Boolean Render( RDDIB ^dib, RDMatrix^mat, Boolean show_annot, RD_RENDER_MODE mode )
			{
				return PDF_Page_render( m_page, dib->m_dib, mat->m_mat, show_annot, (::PDF_RENDER_MODE)mode );
			}
			/**
			 * render page to bmp object. this function returned for cancelled or finished.<br/>before render, you need invoke RenderPrepare.
			 * @param bmp RDBmp object to render.
			 * @param mat Matrix object define scale, rotate, translate operations.
			 * @param mode poor/normal/best.
			 * @return true or false.
			 */
			Boolean RenderToBmp( RDBmp ^bmp, RDMatrix^mat, Boolean show_annot, RD_RENDER_MODE mode )
			{
				return PDF_Page_renderToBmp( m_page, bmp->m_bmp, mat->m_mat, show_annot, (::PDF_RENDER_MODE)mode );
			}
			Boolean RenderToSoftBmp( RDSoftBmp^ bmp, RDMatrix^ mat, Boolean show_annot, RD_RENDER_MODE mode)
			{
				return PDF_Page_renderToBmp(m_page, bmp->m_bmp, mat->m_mat, show_annot, (::PDF_RENDER_MODE)mode);
			}
			/**
			 * set page status to cancelled and cancel render function.
			 */
			void RenderCancel()
			{
				PDF_Page_renderCancel( m_page );
			}
			/**
			 * check if page rendering is finished.
			 * @return true or false
			 */
			Boolean RenderIsFinished()
			{
				return PDF_Page_renderIsFinished( m_page );
			}
			/**
			 * add a font as resource of this page.<br/>
			 * @param font font object created by PDFDoc.NewFontCID()
			 * @return ResFont object or null.
			 */
			PDFResFont ^AddResFont( PDFDocFont ^font )
			{
				PDF_PAGE_FONT pf = PDF_Page_addResFont( m_page, font->m_font );
				if( pf )
				{
					PDFResFont ^font = ref new PDFResFont();
					font->m_font = pf;
					return font;
				}
				else return nullptr;
			}
			/**
			 * add an image as resource of this page.<br/>
			 * @param image image object created by PDFDoc.NewImage() or PDFDoc.NewImageJPEG()
			 * @return PDFResImage object or null.
			 */
			PDFResImage ^AddResImage( PDFDocImage ^image )
			{
				PDF_PAGE_IMAGE pf = PDF_Page_addResImage( m_page, image->m_image );
				if( pf )
				{
					PDFResImage ^font = ref new PDFResImage();
					font->m_image = pf;
					return font;
				}
				else return nullptr;
			}
			/**
			 * add GraphicState as resource of this page.<br/>
			 * @param gstate ExtGraphicState created by PDFDoc.NewGState();
			 * @return PDFResGState object or null.
			 */
			PDFResGState ^AddResGState( PDFDocGState ^gs )
			{
				PDF_PAGE_GSTATE pf = PDF_Page_addResGState( m_page, gs->m_gs );
				if( pf )
				{
					PDFResGState ^font = ref new PDFResGState();
					font->m_gs = pf;
					return font;
				}
				else return nullptr;
			}
			/**
			 * add Form as resource of this page.<br/>
			 * @param form Form created by PDFDoc.NewForm();
			 * @return PDFResForm object, or null if failed.
			 */
			PDFResForm ^AddResForm(PDFDocForm ^form)
			{
				PDF_PAGE_FORM pf = PDF_Page_addResForm(m_page, form->m_form);
				if (pf)
				{
					PDFResForm ^font = ref new PDFResForm();
					font->m_form = pf;
					return font;
				}
				else return nullptr;
			}
			Boolean AddContent(PDFPageContent ^content, Boolean flush)
			{
				return PDF_Page_addContent( m_page, content->m_content, flush );
			}
			/**
			 * get text and image objects to memory.<br/>
			 */
			void ObjsStart()
			{
				PDF_Page_objsStart( m_page );
			}
			/**
			 * get chars count in this page. this can be invoked after ObjsStart<br/>
			 * @return count or 0 if ObjsStart not invoked.
			 */
			int ObjsGetCharCount()
			{
				return PDF_Page_objsGetCharCount(m_page);
			}
			/**
			 * get char's box in PDF coordinate system, this can be invoked after ObjsStart
			 * @param index 0 based unicode index.
			 * @param vals return 4 elements for PDF rectangle.
			 */
			RDRect ObjsGetCharRect( int index )
			{
				RDRect rect;
				PDF_Page_objsGetCharRect( m_page, index, (PDF_RECT *)&rect );
				return rect;
			}
			/**
			 * get char index nearest to point
			 * @param pt point as [x,y] in PDF coordinate.
			 * @return char index or -1 failed.
			 */
			int ObjsGetCharIndex( float x, float y )
			{
				return PDF_Page_objsGetCharIndex( m_page, x, y );
			}
			/**
			 * get index aligned by word. this can be invoked after ObjsStart
			 * @param from 0 based unicode index.
			 * @param dir if dir < 0,  get start index of the word. otherwise get last index of the word.
			 * @return aligned char index
			 */
			int ObjsAlignWord( int index, int dir )
			{
				return PDF_Page_objsAlignWord( m_page, index, dir );
			}
			/**
			 * get font name by char index. this can be invoked after ObjsStart
			 * @param index 0 based unicode index.
			 * @return font name, may be null.
			 */
			String^ ObjsGetCharFontName(int index)
			{
				return cvt_cstr_str(PDF_Page_objsGetCharFontName(m_page, index));
			}
			/**
			 * get string int range[from to]. this can be invoked after ObjsStart.
			 * @param from 0 based unicode index.
			 * @param to 0 based unicode index.
			 * @return string or null.
			 */
			String ^ObjsGetString( int from, int to )
			{
				wchar_t *txt = (wchar_t *)malloc( sizeof( wchar_t ) * (to - from + 3) );
				PDF_Page_objsGetStringW( m_page, from, to, txt, to - from + 2 );
				String ^ret = ref new String( txt );
				free( txt );
				return ret;
			}
			/**
			 * remove chars from PDF page.
			 * @param ranges array of char index pair.<br/>
			 * example: a string "hello_world." pass [0, 2, 6, 8] the result will be "__llo___rld.", where '_' mean the blank char.
			 * @param reload is flush cache data after this invoking? flush not effect saved data, only effect page operator cache.<br/>
			 * if you close this page after this call, pass false to flush shall has better performance.
			 * @return true or false.
			 */
			bool ObjsRemove(const Array<int>^ ranges, bool reload)
			{
				return PDF_Page_objsRemove(m_page, ranges->Data, ranges->Length, reload);
			}
			/**
			 * get image information
			 * @param index range:[0, ObjsGetCharCount()-1]
			 * @return null if char is not image, others: [bits of image, flags, width, height]<br/>
			 * bits of image: bits of image.<br/>
			 * flags: flags&1: mask, flags&2: has palette, flags&4: need interpolate.<br/>
			 * width is image width.<br/>
			 * height is image height.<br/>
			 */
			Array<int>^ ObjsGetImageInfo(int index)
			{
				int ibuf[10];
				if (!PDF_Page_objsGetImageInfo(m_page, index, ibuf)) return nullptr;
				return ref new Array<int>(ibuf, 10);
			}
			/**
			 * set image from jpeg file.
			 * @param index char index, range:[0, ObjsGetCharCount()-1]
			 * @param path same as Document.NewImageJPEG()
			 * @param interpolate interpolate pixels when rendering image scale large than origin image size.
			 * @return true or false, mask image always return false.
			 */
			bool ObjsSetImageJPEG(int index, String^ path, bool interpolate)
			{
				return PDF_Page_objsSetImageJPEG(m_page, index, path, interpolate);
			}
			/**
			 * set image from jpeg byte array.
			 * @param index char index, range:[0, ObjsGetCharCount()-1]
			 * @param data same as Document.NewImageJPEGByMem()
			 * @param len same as Document.NewImageJPEGByMem()
			 * @param interpolate interpolate pixels when rendering image scale large than origin image size.
			 * @return true or false, mask image always return false.
			 */
			bool ObjsSetImageJPEGByMem(int index, const Array<BYTE> ^buf, bool interpolate)
			{
				return PDF_Page_objsSetImageJPEGByMem(m_page, index, buf->Data, buf->Length, interpolate);
			}
			/**
			 * set image from jpx file.
			 * @param index char index, range:[0, ObjsGetCharCount()-1]
			 * @param path same as Document.NewImageJPX()
			 * @param interpolate interpolate pixels when rendering image scale large than origin image size.
			 * @return true or false, mask image always return false.
			 */
			bool ObjsSetImageJPX(int index, String^ path, bool interpolate)
			{
				return PDF_Page_objsSetImageJPX(m_page, index, path, interpolate);
			}
			/**
			 * create a find session. this can be invoked after ObjsStart
			 * @param str key string to find.
			 * @param match_case match case?
			 * @param whole_word match whole word?
			 * @return handle of find session, or 0 if no found.
			 */
			PDFFinder ^GetFinder( String ^key, Boolean match_case, Boolean whole_word )
			{
				PDF_FINDER find = PDF_Page_findOpenW( m_page, key->Data(), match_case, whole_word );
				if( find )
				{
					PDFFinder ^finder = ref new PDFFinder();
					finder->m_finder = find;
					return finder;
				}
				else return nullptr;
			}
			/**
			 * create a find session. this can be invoked after ObjsStart<br/>
			 * this function treats line break as blank char.
			 * @param str key string to find.
			 * @param match_case match case?
			 * @param whole_word match whole word?
			 * @param skip_blank skip blank?
			 * @return handle of find session, or 0 if no found.
			 */
			PDFFinder^ GetFinder(String^ key, Boolean match_case, Boolean whole_word, Boolean skip_blanks)
			{
				PDF_FINDER find = PDF_Page_findOpen2W(m_page, key->Data(), match_case, whole_word, skip_blanks);
				if (find)
				{
					PDFFinder^ finder = ref new PDFFinder();
					finder->m_finder = find;
					return finder;
				}
				else return nullptr;
			}
			/**
			 * get annotations by index.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * @param index 0 based index value. range:[0, GetAnnotCount()-1]
			 * @return handle of annotation, valid until Close invoked.
			 */
			PDFAnnot ^GetAnnot( int index );
			/**
			 * get annotation by PDF point.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * @param x x value in PDF coordinate system.
			 * @param y y value in PDF coordinate system.
			 * @return Annotation object, valid until Page.Close invoked.
			 */
			PDFAnnot ^GetAnnot( float x, float y );
			/**
			 * remove all annotations and display it as normal content on page.<br/>
			 * @return true or false
			 */
			Boolean FlatAnnots()
			{
				return PDF_Page_flate(m_page);
			}
			/**
			 * get annotations count in this page.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 */
			property int AnnotCount
			{
				int get(){return PDF_Page_getAnnotCount(m_page);}
			}
			/**
			 * get rotate degree for page, example: 0 or 90
			 */
			property int Rotate
			{
				int get() { return PDF_Page_getRotate(m_page); }
			}
			Boolean AddAnnot(PDFRef ref, int index)
			{
				return PDF_Page_addAnnot(m_page, ref.ref, index);
			}
			/**
			 * add a text-markup annotation to page.<br/>
			 * you should re-render page to display modified data.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * @param mat Matrix for Render function.
			 * @param rects 4 * n rectangles, each 4 elements: left, top, right, bottom in DIB coordinate system. n is decided by length of array.
			 * @param type 0: Highlight, 1: Underline, 2: StrikeOut, 3: Highlight without round corner.
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotMarkup( int ci1, int ci2, unsigned int color, int type )
			{
				return PDF_Page_addAnnotMarkup2( m_page, ci1, ci2, color, type );
			}
			/**
			 * add page jumping link to page.<br/>
			 * you should render page again to display modified data in viewer.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * @param rect link area rect [left, top, right, bottom] in PDF coordinate.
			 * @param pageno 0 based pageno to jump.
			 * @param top y coordinate in PDF coordinate, page.height is top of page. and 0 is bottom of page.
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotGoto( RDRect rect, int dest, float y )
			{
				return PDF_Page_addAnnotGoto2( m_page, (const PDF_RECT *)&rect, dest, y );
			}
			/**
			 * add URL link to page.<br/>
			 * you should render page again to display modified data in viewer.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * @param rect link area rect [left, top, right, bottom] in PDF coordinate.
			 * @param uri url address, example: "https://www.radaeepdf.com"
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotURI( RDRect rect, String ^uri )
			{
				char *tmp = cvt_str_cstr( uri );
				bool ret = PDF_Page_addAnnotURI2( m_page, (const PDF_RECT *)&rect, tmp );
				free(tmp);
				return ret;
			}
			Boolean AddAnnotPopup(PDFAnnot ^parent, RDRect rect, bool open);
			/**
			 * add a bitmap object as an annotation to page.<br/>
			 * you should render page again to display modified data in viewer.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license, and Document.SetCache() invoked.
			 * @param image DocImage object return from Document.NewImage*();
			 * @param rect 4 elements: left, top, right, bottom in PDF coordinate system.
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotBitmap(PDFDocImage ^img, RDRect rect)
			{
				return PDF_Page_addAnnotBitmap2(m_page, img->m_image, (const PDF_RECT *)&rect);
			}
			Boolean AddAnnotBitmap(PDFDocImage ^img, RDMatrix^mat, RDRect rect)
			{
				return PDF_Page_addAnnotBitmap(m_page, mat->m_mat, img->m_image, (const PDF_RECT *)&rect);
			}
			/**
			 * add an RichMedia annotation to page.<br/>
			 * you should render page again to display modified data.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license, and Document.SetCache invoked.
			 * @param path_player path-name to flash player. example: "/sdcard/VideoPlayer.swf", "/sdcard/AudioPlayer.swf"
			 * @param path_content path-name to RichMedia content. example: "/sdcard/video.mp4", "/sdcard/audio.mp3"
			 * @param type 0: Video, 1: Audio, 2: Flash, 3: 3D<br/>
			 *             Video like *.mpg, *.mp4 ...<br/>
			 *             Audio like *.mp3 ...<br/>
			 * @param image DocImage object return from Document.NewImage*();
			 * @param rect 4 elements: left, top, right, bottom in PDF coordinate system.
			 * @return true or false.
			 */
			Boolean AddAnnotRichMedia(String ^path_player, String ^path_content, int type, PDFDocImage ^img, RDRect rect)
			{
				return PDF_Page_addAnnotRichMedia(m_page, path_player, path_content, type, img->m_image, (const PDF_RECT *)&rect);
			}
			/**
			 * add hand-writing to page.<br/>
			 * you should re-render page to display modified data.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license
			 * @param ink Ink object in PDF coordinate.
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotInk( RDInk ^ink )
			{
				return PDF_Page_addAnnotInk2( m_page, ink->m_ink );
			}
			/**
			 * add polygon to page.<br/>
			 * you should render page again to display modified data.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license
			 * @param path must be a closed contour.
			 * @param color stroke color formated as 0xAARRGGBB.
			 * @param fill_color fill color, formated as 0xAARRGGBB. if AA == 0, no fill operations, otherwise alpha value is same to stroke color.
			 * @param width stroke width in PDF coordinate
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotPolygon(RDPath ^path, unsigned int color, unsigned int fill_color, float width)
			{
				if (!path) return false;
				return PDF_Page_addAnnotPolygon(m_page, path->m_path, color, fill_color, width);
			}
			/**
			 * add polyline to page.<br/>
			 * you should re-render page to display modified data.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license
			 * @param path must be a set of unclosed lines. do not container any move-to operation except the first point in the path.
			 * @param style1 style for start point:<br/>
			 * 0: None<br/>
			 * 1: Arrow<br/>
			 * 2: Closed Arrow<br/>
			 * 3: Square<br/>
			 * 4: Circle<br/>
			 * 5: Butt<br/>
			 * 6: Diamond<br/>
			 * 7: Reverted Arrow<br/>
			 * 8: Reverted Closed Arrow<br/>
			 * 9: Slash
			 * @param style2 style for end point, values are same as style1.
			 * @param color stroke color formated as 0xAARRGGBB.
			 * @param fill_color fill color, formated as 0xAARRGGBB. if AA == 0, no fill operations, otherwise alpha value is same to stroke color.
			 * @param width stroke width in PDF coordinate
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotPolyline(RDPath ^path, unsigned int color, int style1, int style2, unsigned int fill_color, float width)
			{
				if (!path) return false;
				return PDF_Page_addAnnotPolyline(m_page, path->m_path, style1, style2, color, fill_color, width);
			}
			/**
			 * add line to page.<br/>
			 * you should render page again to display modified data.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license
			 * @param pt1 start point in PDF coordinate, 2 elements for x,y
			 * @param pt2 end point in PDF coordinate, 2 elements for x,y
			 * @param style1 style for start point:<br/>
			 * 0: None<br/>
			 * 1: Arrow<br/>
			 * 2: Closed Arrow<br/>
			 * 3: Square<br/>
			 * 4: Circle<br/>
			 * 5: Butt<br/>
			 * 6: Diamond<br/>
			 * 7: Reverted Arrow<br/>
			 * 8: Reverted Closed Arrow<br/>
			 * 9: Slash
			 * @param style2 style for end point, values are same as style1.
			 * @param width line width in DIB coordinate
			 * @param color line color. same as addAnnotRect.
			 * @param icolor fill color, used to fill arrows of the line.
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotLine( float x1, float y1, float x2, float y2, int style1, int style2, float width, unsigned int color, unsigned int icolor )
			{
				PDF_POINT pt1;
				PDF_POINT pt2;
				pt1.x = x1;
				pt1.y = y1;
				pt2.x = x2;
				pt2.y = y2;
				return PDF_Page_addAnnotLine2( m_page, &pt1, &pt2, style1, style2, width, color, icolor );
			}
			/**
			 * add rectangle to page.<br/>
			 * you should render page again to display modified data.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license
			 * @param rect 4 elements for left, top, right, bottom in PDF coordinate system
			 * @param width line width in PDF coordinate.
			 * @param color rectangle color, formated as 0xAARRGGBB
			 * @param icolor fill color in rectangle, formated as 0xAARRGGBB, if alpha channel is 0, means no fill operation, otherwise alpha channel are ignored.
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotRect( RDRect rect, float width, unsigned int color, unsigned int icolor )
			{
				return PDF_Page_addAnnotRect2( m_page, (const PDF_RECT *)&rect, width, color, icolor );
			}
			/**
			 * add ellipse to page.<br/>
			 * you should render page again to display modified data.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license
			 * @param rect 4 elements for left, top, right, bottom in PDF coordinate system
			 * @param width line width in PDF coordinate
			 * @param color ellipse color, formated as 0xAARRGGBB
			 * @param icolor fill color in ellipse, formated as 0xAARRGGBB, if alpha channel is 0, means no fill operation, otherwise alpha channel are ignored.
			 * @return true or false<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotEllipse( RDRect rect, float width, unsigned int color, unsigned int icolor )
			{
				return PDF_Page_addAnnotEllipse2( m_page, (const PDF_RECT *)&rect, width, color, icolor );
			}
			/**
			 * add a sticky text annotation to page.<br/>
			 * you should render page again to display modified data.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license
			 * @param pt 2 elements: x, y in PDF coordinate system.
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotTextNote( float x, float y )
			{
				return PDF_Page_addAnnotText2( m_page, x, y );
			}
			/**
			 * add an edit-box on page.<br/>
			 * the font of edit box is set by Global.setTextFont in Global.Init().<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require premium license.
			 * @param mat Matrix object that passed to Render or RenderToBmp function.
			 * @param rect 4 elements: left, top, right, bottom in PDF coordinate system.
			 * @param line_clr color of border line, formated as 0xAARRGGBB.
			 * @param line_w width of border line.
			 * @param fill_clr color of background, formated as 0xAARRGGBB. AA must same to line_clr AA, or 0 means no fill color.
			 * @param tsize text size in DIB coordinate system.
			 * @param text_clr text color, formated as 0xAARRGGBB. AA must same to line_clr AA
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotEditbox( RDRect rect, unsigned int line_clr, float line_w, unsigned int fill_clr, float tsize, unsigned int text_clr )
			{
				return PDF_Page_addAnnotEditbox2( m_page, (const PDF_RECT *)&rect, line_clr, line_w, fill_clr, tsize, text_clr );
			}
			/**
			 * add a file as an attachment to page.<br/>
			 * you should render page again to display modified data in viewer.<br/>
			 * this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
			 * this method require professional or premium license, and Document.SetCache invoked.
			 * @param path absolute path name to the file.
			 * @param icon icon display to the page. values as:<br/>
			 * 0: PushPin<br/>
			 * 1: Graph<br/>
			 * 2: Paperclip<br/>
			 * 3: Tag<br/>
			 * @param rect 4 elements: left, top, right, bottom in PDF coordinate system.
			 * @return true or false.<br/>
			 * the added annotation can be obtained by Page.GetAnnot(Page.GetAnnotCount() - 1), if this method return true.
			 */
			Boolean AddAnnotAttachment( RDRect rect, String ^path, int icon )
			{
				char *tmp = cvt_str_cstr(path);
				bool ret = PDF_Page_addAnnotAttachment( m_page, tmp, icon, (const PDF_RECT *)&rect );
				free( tmp );
				return ret;
			}
			property int PGEditorNodeCount
			{
				int get() { return PDF_Page_getPGEditorNodeCount(m_page); }
			}

			/// <summary>
			/// Set if page editor has been modified
			/// </summary>
			/// <param name="modified">True or false</param>
			void SetPGEditorModified(bool modified)
			{
				PDF_Page_setPGEditorModified(m_page, modified);
			}
			/// <summary>
			/// Get specified page editor node
			/// </summary>
			/// <param name="index">0 based index of target node</param>
			/// <returns>A PDFEditNode object if found</returns>
			PDFEditNode^ GetPGEditorNode(int index)
			{
				PDF_EDITNODE node = PDF_Page_getPGEditorNode1(m_page, index);
				if (!node) return nullptr;
				return ref new PDFEditNode(node);
			}
			/// <summary>
			/// Get page editor node at specified position
			/// </summary>
			/// <param name="pdfx">x pdf coordinate of the position</param>
			/// <param name="pdfy">y pdf coordinate of the position</param>
			/// <returns>A PDFEditNode object if found</returns>
			PDFEditNode^ GetPGEditorNode(float pdfx, float pdfy)
			{
				PDF_EDITNODE node = PDF_Page_getPGEditorNode2(m_page, pdfx, pdfy);
				if (!node) return nullptr;
				return ref new PDFEditNode(node);
			}
			/// <summary>
			/// Render page with page editor
			/// </summary>
			/// <param name="dib">A RDDIB which the page will be rendered to</param>
			/// <param name="mat">A RDMatrix which will be used for coordinates transform and scale</param>
			/// <param name="quality">Render quality</param>
			/// <returns>True if successed, otherwise false</returns>
			bool RenderWithPGEditor(RDDIB^ dib, RDMatrix^ mat, int quality)
			{
				return PDF_Page_renderWithPGEditor(m_page, dib->m_dib, mat->m_mat, true, quality);
			}
			/// <summary>
			/// Update page with page editor
			/// </summary>
			/// <returns>True if successed, otherwise false</returns>
			bool UpdateWithPGEditor()
			{
				return PDF_Page_updateWithPGEditor(m_page);
			}
			/// <summary>
			/// Cancel rendering with page editor
			/// </summary>
			/// <returns>True if successed, otherwise false</returns>
			bool CancelWithPGEditor()
			{
				return PDF_Page_cancelWithPGEditor(m_page);
			}
			/**
			 * add a checkbox field.<br/>
			 * this method require premium license.
			 * @param rect [left, top, right, bottom] in PDF coordinate system.
			 * @param name part field name, example "fld1"
			 * @param val the value to export when the checkbox is selected.
			 * @param app_on appearence for on status. if it is null, a default appearence will be generated by native.
			 * @param app_off appearence fof off status. if it is null, a default appearence will be generated by native.
			 * @return
			 */
			Boolean AddFieldCheck(RDRect rect, String^ name, String^ val, PDFDocForm ^app_on, PDFDocForm ^app_off)
			{
				return PDF_Page_addFieldCheck(m_page, (const PDF_RECT*)&rect, name, val, (app_on) ? app_on->m_form : NULL, (app_off) ? app_off->m_form : NULL);
			}
			/**
			 * add an radiobox field.<br/>
			 * this method require premium license.
			 * @param rect [left, top, right, bottom] in PDF coordinate system.
			 * @param name part field name, example "fld1", if the name already exists, then the field is a group, and this new radio add to this radio group.
			 * @param val the value to export when the checkbox is selected.
			 * @param app_on appearence for on status. if it is null, a default appearence will be generated by native.
			 * @param app_off appearence fof off status. if it is null, a default appearence will be generated by native.
			 * @return
			 */
			Boolean AddFieldRadio(RDRect rect, String^ name, String^ val, PDFDocForm^ app_on, PDFDocForm^ app_off)
			{
				return PDF_Page_addFieldRadio(m_page, (const PDF_RECT*)&rect, name, val, (app_on) ? app_on->m_form : NULL, (app_off) ? app_off->m_form : NULL);
			}
			/**
			 * add a Combo field.
			 * this method require premium license.
			 * @param rect [left, top, right, bottom] in PDF coordinate system.
			 * @param name part field name, example "fld1"
			 * @param opts option items to select.
			 * @return
			 */
			Boolean AddFieldCombo(RDRect rect, String^ name, const Array<String ^> ^opts)
			{
				return PDF_Page_addFieldCombo(m_page, (const PDF_RECT*)&rect, name, opts);
			}
			/**
			 * add a list field.
			 * this method require premium license.
			 * @param rect [left, top, right, bottom] in PDF coordinate system.
			 * @param name part field name, example "fld1"
			 * @param opts option items to select.
			 * @param multi_sel is the list can selected more than 1 item?
			 * @return
			 */
			Boolean AddFieldList(RDRect rect, String^ name, const Array<String^>^ opts, bool multi_sel)
			{
				return PDF_Page_addFieldList(m_page, (const PDF_RECT*)&rect, name, opts, multi_sel);
			}
			/**
			 * add an editbox field.<br/>
			 * this method require premium license.
			 * @param rect [left, top, right, bottom] in PDF coordinate system.
			 * @param name part field name, example "fld1"
			 * @param multi_line is the editbox multi-line?
			 * @param password is the editbox password?
			 * @return true or false.
			 */
			Boolean AddFieldEditbox(RDRect rect, String^ name, bool multi_line, bool password)
			{
				return PDF_Page_addFieldEditbox(m_page, (const PDF_RECT*)&rect, name, multi_line, password);
			}
			/**
			 * add an empty signature field.
			 * @param rect [left, top, right, bottom] in PDF coordinate system.
			 * @param name part field name, example "fld1"
			 * @return
			 */
			Boolean AddFieldSign(RDRect rect, String^ name)
			{
				return PDF_Page_addFieldSign(m_page, (const PDF_RECT*)&rect, name);
			}
			/**
			 * new a tag with predefined name.
			 * @param parent group tag return from Document.NewTagGroup();
			 * @param tag predefined block tag name, defined in PDF-Reference-1.7, chapter 10.7.3, TABLE 10.21.
			 * @return the return value can used in PageContent object.and the object is related to page object.
			 */
			PDFPageTag^ NewTagBlock(PDFDocTag^ parent, String^ stag)
			{
				PDF_TAG ret = NULL;
				if(parent) ret = PDF_Page_newTagBlock(m_page, parent->m_tag, stag);
				else ret = PDF_Page_newTagBlock(m_page, NULL, stag);
				if (!ret) return nullptr;
				return ref new PDFPageTag(ret);
			}
			/// <summary>
			/// Close current page
			/// </summary>
			void Close()
			{
				if (!m_ref && m_page)
					PDF_Page_close(m_page);
				m_page = NULL;
				m_ref = false;
			}
			/// <summary>
			/// Advanced function to get reference of annotation object.
			/// This method require premium license.
			/// </summary>
			/// <returns>Reference of the page object</returns>
			PDFRef Advance_GetRef()
			{
				PDF_OBJ_REF ref = PDF_Page_advGetRef(m_page);
				PDFRef ret;
				ret.ref = ref;
				return ret;
			}
			/// <summary>
			/// Advanced function to reload annotation object, after advanced methods update annotation object data.
			/// This method require premium license.
			/// </summary>
			void Advance_Reload()
			{
				PDF_Page_advReload(m_page);
			}
			/**
			 * Sign and save the PDF file.<br/>
			 * this method required premium license, and signed feature native libs, which has bigger size.
			 * @param form appearance for sign field.
			 * @param rect rectangle for sign field
			 * @param cert_file a cert file like .p12 or .pfx file, DER encoded cert file.
			 * @param pswd password to open cert file.
			 * @param name signer name.
			 * @param reason sign reason will write to signature.
			 * @param location signature location will write to signature.
			 * @param contact contact info will write to signature.
			 * @return 0 mean OK, others are failed.
			 */
			int Sign(PDFDocForm ^form, RDRect rect, String ^cert_file, String ^pswd, String ^name, String ^reason, String ^location, String ^contact)
			{
				char *ccert_file = cvt_str_cstr(cert_file);
				char *cpswd = cvt_str_cstr(pswd);
				char* cname = cvt_str_cstr(name);
				char *creason = cvt_str_cstr(reason);
				char *clocation = cvt_str_cstr(location);
				char *ccontact = cvt_str_cstr(contact);
				return PDF_Page_sign(m_page, form->m_form, (const PDF_RECT *)&rect, ccert_file, cpswd, cname, creason, clocation, ccontact);
				free(ccontact);
				free(clocation);
				free(creason);
				free(cname);
				free(cpswd);
				free(ccert_file);
			}
		private:
			PDFPage()
			{
				//m_doc = nullptr;
				m_page = NULL;
				m_ref = false;
			}
			~PDFPage()
			{
				Close();
			}
			friend PDFDoc;
			friend PDFAnnot;
			bool m_ref;
			//PDFDoc ^m_doc;
			PDF_PAGE m_page;
		};
		public ref class PDFAnnot sealed
		{
		public:
			/**
			 * annotation type. type as following values:<br/>
			 * 0:  unknown<br/>
			 * 1:  text<br/>
			 * 2:  link<br/>
			 * 3:  free text<br/>
			 * 4:  line<br/>
			 * 5:  square<br/>
			 * 6:  circle<br/>
			 * 7:  polygon<br/>
			 * 8:  polyline<br/>
			 * 9:  text hilight<br/>
			 * 10: text under line<br/>
			 * 11: text squiggly<br/>
			 * 12: text strikeout<br/>
			 * 13: stamp<br/>
			 * 14: caret<br/>
			 * 15: ink<br/>
			 * 16: popup<br/>
			 * 17: file attachment<br/>
			 * 18: sound<br/>
			 * 19: movie<br/>
			 * 20: widget<br/>
			 * 21: screen<br/>
			 * 22: print mark<br/>
			 * 23: trap net<br/>
			 * 24: water mark<br/>
			 * 25: 3d object<br/>
			 * 26: rich media
			 */
			property int Type
			{
				int get(){return PDF_Page_getAnnotType(m_page->m_page, m_annot);}
			}
			/**
			 * get annotation field type in acroForm. type as these values:<br/>
			 * 0: unknown<br/>
			 * 1: button field<br/>
			 * 2: text field<br/>
			 * 3: choice field<br/>
			 * 4: signature field<br/>
			 */
			property int FieldType
			{
				int get(){return PDF_Page_getAnnotFieldType(m_page->m_page, m_annot);}
			}
			/**
			 * get name of the annotation without NO. a fields group with same name "field#0","field#1"��got to "field".
			 * example: "EditBox1[0]".
			 */
			property String ^FieldName
			{
				String ^get()
				{
					wchar_t tmp[512] = {0};
					if( PDF_Page_getAnnotFieldNameW(m_page->m_page, m_annot, tmp, 511 ) <= 0 ) return nullptr;
					else return ref new String( tmp );
				}
			}
			/**
			 * get name of the annotation.
			 * example: "EditBox1[0]".
			 */
			property String ^FieldNameWithNO
			{
				String ^get()
				{
					wchar_t tmp[512] = { 0 };
					if (PDF_Page_getAnnotFieldNameWithNOW(m_page->m_page, m_annot, tmp, 511) <= 0) return nullptr;
					else return ref new String(tmp);
				}
			}
			/**
			 * get name of the annotation.
			 * example: "Form1.EditBox1".
			 */
			property String ^FieldFullName
			{
				String ^get()
				{
					wchar_t tmp[512] = {0};
					if( PDF_Page_getAnnotFieldFullNameW(m_page->m_page, m_annot, tmp, 511 ) <= 0 ) return nullptr;
					else return ref new String( tmp );
				}
			}
			/**
			 * get full name of the annotation with more details.<br/>
			 * example: "Form1[0].EditBox1[0]".
			 */
			property String ^FieldFullName2
			{
				String ^get()
				{
					wchar_t tmp[512] = {0};
					if( PDF_Page_getAnnotFieldFullName2W(m_page->m_page, m_annot, tmp, 511 ) <= 0 ) return nullptr;
					else return ref new String( tmp );
				}
			}
			/**
			 * get jsvascript action of fields.<br/>
			 * this method require premium license.
			 * @param idx action index:<br/>
			 *            0:'K' performed when the user types a keystroke<br/>
			 *            1:'F' to be performed before the field is formatted to display its current value.<br/>
			 *            2:'V' to be performed when the field��s value is changed<br/>
			 *            3:'C' to be performed to recalculate the value of this field when that of another field changes.<br/>
			 * @return javsscript of field's action<br/>
			 */
			String ^GetFieldJS(int idx)
			{
				return PDF_Page_getAnnotFieldJS(m_page->m_page, m_annot, idx);
			}
			/**
			 * is position and size of the annotation locked?<br/>
			 */
			property Boolean Locked
			{
				Boolean get(){return PDF_Page_isAnnotLocked(m_page->m_page, m_annot);}
				void set(Boolean val) { PDF_Page_setAnnotLock(m_page->m_page, m_annot, val); }
			}
			/**
			 * is texts of the annotation locked?<br/>
			 */
			property Boolean LockedContent
			{
				Boolean get(){return PDF_Page_isAnnotLockedContent(m_page->m_page, m_annot);}
			}
			/**
			* is the annotation hide.
			*/
			property Boolean Hide
			{
				Boolean get(){return PDF_Page_isAnnotHide(m_page->m_page, m_annot);}
				void set(Boolean val){ PDF_Page_setAnnotHide( m_page->m_page, m_annot, val ); }
			}
			/**
			* is the annotation readonly.
			*/
			property Boolean ReadOnly
			{
				Boolean get() { return PDF_Page_isAnnotReadOnly(m_page->m_page, m_annot); }
				void set(Boolean val) { PDF_Page_setAnnotReadOnly(m_page->m_page, m_annot, val); }
			}
			/**
			 * annotation's box rectangle.<br/>
			 */
			property RDRect Rect
			{
				RDRect get(){RDRect rect; PDF_Page_getAnnotRect(m_page->m_page, m_annot, (PDF_RECT *)&rect); return rect;}
				void set( RDRect rect ){ PDF_Page_setAnnotRect( m_page->m_page, m_annot, (const PDF_RECT *)&rect);}
			}
			/**
			 * fill color of square/circle/highlight/line/ploygon/polyline/sticky text/free text/text field annotation.<br/>
			 * color value formatted as 0xAARRGGBB, if 0 returned, means false.
			 */
			property int FillColor
			{
				int get(){return PDF_Page_getAnnotFillColor(m_page->m_page, m_annot);}
				void set( int color ){ PDF_Page_setAnnotFillColor(m_page->m_page, m_annot, color);}
			}
			/**
			 * stroke color of square/circle/ink/line/underline/Squiggly/strikeout/ploygon/polyline/free text/text field annotation.<br/>
			 * color value formatted as 0xAARRGGBB, if 0 returned, means false.
			 */
			property int StrokeColor
			{
				int get(){return PDF_Page_getAnnotStrokeColor(m_page->m_page, m_annot);}
				void set( int color ){ PDF_Page_setAnnotStrokeColor(m_page->m_page, m_annot, color);}
			}
			/**
			 * stroke width of square/circle/ink/line/ploygon/polyline/free text/text field annotation.<br/>
			 * for free text annotation: width of edit-box border<br/>
			 */
			property float StrokeWidth
			{
				float get(){return PDF_Page_getAnnotStrokeWidth(m_page->m_page, m_annot);}
				void set( float val ){ PDF_Page_setAnnotStrokeWidth(m_page->m_page, m_annot, val);}
			}
			/**
			 * RDPath object from Ink annotation.<br/>
			 */
			property RDPath ^InkPath
			{
				RDPath ^get()
				{
					PDF_PATH path = PDF_Page_getAnnotInkPath(m_page->m_page, m_annot);
					if (!path) return nullptr;
					return ref new RDPath(path);
				}
				void set(RDPath ^path)
				{
					PDF_Page_setAnnotInkPath(m_page->m_page, m_annot, path->m_path);
				}
			}
			/**
			 * RDPath object from Polygon annotation.<br/>
			 */
			property RDPath ^PolygonPath
			{
				RDPath ^get()
				{
					PDF_PATH path = PDF_Page_getAnnotPolygonPath(m_page->m_page, m_annot);
					if (!path) return nullptr;
					return ref new RDPath(path);
				}
				void set(RDPath ^path)
				{
					PDF_Page_setAnnotPolygonPath(m_page->m_page, m_annot, path->m_path);
				}
			}
			/**
			 * RDPath object from Polyline annotation.<br/>
			 */
			property RDPath ^PolylinePath
			{
				RDPath ^get()
				{
					PDF_PATH path = PDF_Page_getAnnotPolylinePath(m_page->m_page, m_annot);
					if (!path) return nullptr;
					return ref new RDPath(path);
				}
				void set(RDPath ^path)
				{
					PDF_Page_setAnnotPolylinePath(m_page->m_page, m_annot, path->m_path);
				}
			}
			/**
			 * get line style of line or polyline annotation.<br/>
			 * (LineStyle >> 16) is style of end point, (LineStyle & 0xffff) is style of start point.
			 * both start or end style has following values:<br/>
			 * 1:OpenArrow<br/>
			 * 2:ClosedArrow<br/>
			 * 3:Square<br/>
			 * 4:Circle<br/>
			 * 5:Butt<br/>
			 * 6:Diamond<br/>
			 * 7:ROpenArrow<br/>
			 * 8:RClosedArrow<br/>
			 * 9:Slash
			 */
			property int LineStyle
			{
				int get() { return PDF_Page_getAnnotLineStyle(m_page->m_page, m_annot); }
				void set(int val) { PDF_Page_setAnnotLineStyle(m_page->m_page, m_annot, val); }
			}
			/**
			 * icon for sticky text note/file attachment/Rubber Stamp annotation. the value depends on annotation type.
			 * <strong>For sticky text note:</strong><br/>
			 * 0: Note<br/>
			 * 1: Comment<br/>
			 * 2: Key<br/>
			 * 3: Help<br/>
			 * 4: NewParagraph<br/>
			 * 5: Paragraph<br/>
			 * 6: Insert<br/>
			 * 7: Check<br/>
			 * 8: Circle<br/>
			 * 9: Cross<br/>
			 * <strong>For file attachment:</strong><br/>
			 * 0: PushPin<br/>
			 * 1: Graph<br/>
			 * 2: Paperclip<br/>
			 * 3: Tag<br/>
			 * <strong>For Rubber Stamp:</strong><br/>
			 *  0: "Draft"(default icon)<br/>
			 *  1: "Approved"<br/>
			 *  2: "Experimental"<br/>
			 *  3: "NotApproved"<br/>
			 *  4: "AsIs"<br/>
			 *  5: "Expired"<br/>
			 *  6: "NotForPublicRelease"<br/>
			 *  7: "Confidential"<br/>
			 *  8: "Final"<br/>
			 *  9: "Sold"<br/>
			 * 10: "Departmental"<br/>
			 * 11: "ForComment"<br/>
			 * 12: "TopSecret"<br/>
			 * 13: "ForPublicRelease"<br/>
			 * 14: "Accepted"<br/>
			 * 15: "Rejected"<br/>
			 * 16: "Witness"<br/>
			 * 17: "InitialHere"<br/>
			 * 18: "SignHere"<br/>
			 * 19: "Void"<br/>
			 * 20: "Completed"<br/>
			 * 21: "PreliminaryResults"<br/>
			 * 22: "InformationOnly"<br/>
			 * 23: "End"<br/>
			 * @return true or false.
			 */
			property int Icon
			{
				int get(){return PDF_Page_getAnnotIcon(m_page->m_page, m_annot);}
				void set( int icon ){ PDF_Page_setAnnotIcon(m_page->m_page, m_annot, icon);}
			}
			/**
			 * annotation's destination. 0 based page NO, or -1 if failed.
			 */
			property int Dest
			{
				int get(){return PDF_Page_getAnnotDest(m_page->m_page, m_annot);}
			}
			/**
			* annotation's java-script string.
			*/
			property String ^JS
			{
				String ^get() {return PDF_Page_getAnnotJS(m_page->m_page, m_annot);}
			}
			/**
			* is this annotation URI link?
			*/
			property bool IsURI
			{
				bool get()
				{
					String^ ret = PDF_Page_getAnnotURI(m_page->m_page, m_annot);
					return (ret != nullptr && !ret->IsEmpty());
				}
			}
			/**
			* URI string.
			*/
			property String ^URI
			{
				String ^get()
				{
					return PDF_Page_getAnnotURI(m_page->m_page, m_annot);
				}
			}
			/**
			* is this annotation File link?
			*/
			property bool IsFileLink
			{
				bool get()
				{
					String ^ret = PDF_Page_getAnnotFileLink(m_page->m_page, m_annot);
					return (ret != nullptr && !ret->IsEmpty());
				}
			}
			/// <summary>
			/// Get annotation's file link string.
			/// This method require professional or premium license
			/// return string of URL, or null
			/// </summary>
			property String ^FileLink
			{
				String ^get()
				{
					return PDF_Page_getAnnotFileLink(m_page->m_page, m_annot);
				}
			}
			/// <summary>
			/// Check if the annotation remote link annotation
			/// </summary>
			property bool IsRemoteDest
			{
				bool get()
				{
					String^ ret = PDF_Page_getAnnotRemoteDest(m_page->m_page, m_annot);
					return (ret != nullptr && !ret->IsEmpty());
				}
			}
			/// <summary>
			/// Get annotation's remote link string.
			/// This method require professional or premium license
			/// return string of URL, or null
			/// </summary>
			property String ^RemoteDest
			{
				String ^get()
				{
					return PDF_Page_getAnnotRemoteDest(m_page->m_page, m_annot);
				}
			}
			/// <summary>
			/// Get index of this annotation in page.
			/// return 0 based index value or -1;
			/// </summary>
			property int IndexInPage
			{
				int get()
				{
					int cur = 0;
					int cnt = m_page->AnnotCount;
					while( cur < cnt )
					{
						PDF_ANNOT tmp = PDF_Page_getAnnot( m_page->m_page, cur );
						if( tmp == m_annot ) return cur;
						cur++;
					}
					return -1;
				}
			}
			/**
			 * move annotation to another page.<br/>
			 * this method require professional or premium license.<br/>
			 * this method just like invoke Page.CopyAnnot() and Annotation.RemoveFromPage(), but, faster and less memory allocateed.<br/>
			 * Notice: ObjsStart or RenderXXX shall be invoked for dst_page.
			 * @param dst_page page to move.
			 * @param rect [left, top, right, bottom] in PDF coordinate in dst_page.
			 * @return true or false.
			 */
			Boolean MoveToPage( PDFPage ^page, RDRect rect )
			{
				if( !page || !m_page ) return false;
				return PDF_Page_moveAnnot( m_page->m_page, page->m_page, m_annot, (const PDF_RECT *)&rect );
			}
			/**
			 * remove annotation.<br/>
			 * you should render page again to display modified data in viewer.<br/>
			 * this method require professional or premium license
			 * @return true or false
			 */
			Boolean RemoveFromPage()
			{
				bool ret = PDF_Page_removeAnnot( m_page->m_page, m_annot );
				if( ret )
				{
					m_page = nullptr;
					m_annot = NULL;
				}
				return ret;
			}
			/**
			 * remove this annotation, and display as normal content in page.
			 * @return true of false.
			 */
			Boolean FlateFromPage()
			{
				bool ret = PDF_Page_flateAnnot(m_page->m_page, m_annot);
				if (ret)
				{
					m_page = nullptr;
					m_annot = NULL;
				}
				return ret;
			}
			/**
			 * render page to Bitmap object directly. this function returned for cancelled or finished.<br/>
			 * before render, you need erase Bitmap object.
			 * @param bitmap Bitmap object to render.
			 * @param mat Matrix object define scale, rotate, translate operations.
			 * @return true or false.
			 */
			Boolean RenderToBmp(RDBmp ^bmp)
			{
				if (!bmp || !m_page) return false;
				return PDF_Page_renderAnnotToBmp(m_page->m_page, m_annot, bmp->m_bmp);
			}
			/// <summary>
			/// Check if this annotation is a movie annotation
			/// </summary>
			property bool IsMovie
			{
				bool get()
				{
					String^ ret = PDF_Page_getAnnotMovie(m_page->m_page, m_annot);
					return (ret != nullptr && !ret->IsEmpty());
				}
			}
			/// <summary>
			/// Get annotation's movie name.
			/// This method require professional or premium license
			/// </summary>
			/// <returns>Name of the movie, or null</returns>
			String ^GetMovieName()
			{
				return PDF_Page_getAnnotMovie(m_page->m_page, m_annot);
			}
			/// <summary>
			/// Get annotation's movie data, and save to file.
			/// This method require professional or premium license
			/// </summary>
			/// <param name="save_path">Full path name to save data.</param>
			/// <returns>True if save_file created, or false.</returns>
			Boolean GetMovieData( String ^save_path )
			{
				char *tmp = cvt_str_cstr( save_path );
				bool ret = PDF_Page_getAnnotMovieData( m_page->m_page, m_annot, tmp );
				free( tmp );
				return ret;
			}
			/// <summary>
			/// Check if this annotation is a 3D annotation
			/// </summary>
			property bool Is3D
			{
				bool get()
				{
					String^ ret = PDF_Page_getAnnot3D(m_page->m_page, m_annot);
					return (ret != nullptr && !ret->IsEmpty());
				}
			}
			/// <summary>
			/// Get annotation's 3D component name.
			/// This method require professional or premium license
			/// </summary>
			/// <returns>Name of the 3D component, or null</returns>
			String ^Get3DName()
			{
				return PDF_Page_getAnnot3D( m_page->m_page, m_annot);
			}
			/// <summary>
			/// Get annotation's 3D component data, and save to file.
			/// This method require professional or premium license
			/// </summary>
			/// <param name="save_path">Full path name to save data.</param>
			/// <returns>True if save_file created, or false.</returns>
			Boolean Get3DData( String ^save_path )
			{
				char *tmp = cvt_str_cstr( save_path );
				bool ret = PDF_Page_getAnnot3DData( m_page->m_page, m_annot, tmp );
				free( tmp );
				return ret;
			}
			/// <summary>
			/// Check if this annotation is an attachment annotation
			/// </summary>
			property bool IsAttachment
			{
				bool get()
				{
					String^ ret = PDF_Page_getAnnotAttachment(m_page->m_page, m_annot);
					return (ret != nullptr && !ret->IsEmpty());
				}
			}
			/// <summary>
			/// Get annotation's attachment name.
			/// This method require professional or premium license
			/// </summary>
			/// <returns>Name of the attachment</returns>
			String ^GetAttachmentName()
			{
				return PDF_Page_getAnnotAttachment(m_page->m_page, m_annot);
			}
			/// <summary>
			/// Get annotation's attachment data, and save to file.
			/// This method require professional or premium license
			/// </summary>
			/// <param name="save_path">Full path name to save data.</param>
			/// <returns>True if save_file created, or false.</returns>
			Boolean GetAttachmentData( String ^save_path )
			{
				char *tmp = cvt_str_cstr( save_path );
				bool ret = PDF_Page_getAnnotAttachmentData( m_page->m_page, m_annot, tmp );
				free( tmp );
				return ret;
			}
			/// <summary>
			/// Get annotation's rendition name.
			/// This method require professional or premium license
			/// </summary>
			/// <returns>Name of the rendition</returns>
			String^ GetRenditionName()
			{
				return PDF_Page_getAnnotRendition(m_page->m_page, m_annot);
			}
			/// <summary>
			/// Get annotation's rendition data, and save to file.
			/// This method require professional or premium license
			/// </summary>
			/// <param name="save_path">Full path name to save data.</param>
			/// <returns>True if save_file created, or false.</returns>
			Boolean GetRenditionData(String^ save_path)
			{
				char* tmp = cvt_str_cstr(save_path);
				bool ret = PDF_Page_getAnnotRenditionData(m_page->m_page, m_annot, tmp);
				free(tmp);
				return ret;
			}
			/// <summary>
			/// Check if the annotation is a sound annotation
			/// </summary>
			property bool IsSound
			{
				bool get()
				{
					String^ ret = PDF_Page_getAnnotSound(m_page->m_page, m_annot);
					return (ret != nullptr && !ret->IsEmpty());
				}
			}
			/// <summary>
			/// Get annotation's sound name.
			/// This method require professional or premium license
			/// </summary>
			/// <returns>Name of the sound</returns>
			String ^GetSoundName()
			{
				return PDF_Page_getAnnotSound(m_page->m_page, m_annot);
			}
			/// <summary>
			/// Get annotation's sound data, and save to file.
			/// This method require professional or premium license
			/// </summary>
			/// <param name="save_path">Full path name to save data.</param>
			/// <returns>True if save_file created, or false.</returns>
			Array<int> ^GetSoundData( String ^save_path )
			{
				int paras[6];
				char *tmp = cvt_str_cstr( save_path );
				bool ret = PDF_Page_getAnnotSoundData( m_page->m_page, m_annot, paras, tmp );
				free( tmp );
				if( !ret ) return nullptr;
				else
				{
					Array<int> ^tmp = ref new Array<int>(6);
					memcpy( tmp->Data, paras, sizeof(int) * 6 );
					return tmp;
				}
			}
			/**
			 * item count of RichMedia annotation.<br/>
			 * count of items, or -1 if not RichMedia annotation, and no premium license actived.
			 */
			property int RichMediaItemCount
			{
				int get()
				{
					return PDF_Page_getAnnotRichMediaItemCount(m_page->m_page, m_annot);
				}
			}
			/**
			 * actived item of RichMedia annotation.<br/>
			 * index of actived item, or -1 if not RichMedia annotation, and no premium license actived.
			 */
			property int RichMediaItemActived
			{
				int get()
				{
					return PDF_Page_getAnnotRichMediaItemActived(m_page->m_page, m_annot);
				}
			}
			/**
			 * get content type of an item of RichMedia annotation.<br/>
			 * @param idx range in [0, Annot.GetRichMediaItemCount())
			 * @return type of item:<br/>
			 * -1: unknown or error.<br/>
			 * 0: Video.<br/>
			 * 1��Sound.<br/>
			 * 2:Flash file object.<br/>
			 * 3:3D file object.<br/>
			 */
			int GetRichMediaItemType(int idx)
			{
				return PDF_Page_getAnnotRichMediaItemType(m_page->m_page, m_annot, idx);
			}
			/**
			 * get asset name of content of an item of RichMedia annotation.<br/>
			 * @param idx range in [0, Annot.GetRichMediaItemCount())
			 * @return asset name, or null. example: "VideoPlayer.swf"
			 */
			String ^GetRichMediaItemAsset(int idx)
			{
				return PDF_Page_getAnnotRichMediaItemAsset(m_page->m_page, m_annot, idx);
			}
			/**
			 * get parameters of an item of RichMedia annotation.<br/>
			 * @param idx range in [0, Annot.GetRichMediaItemCount())
			 * @return parameter string, or null.<br/>
			 * example: "source=myvideo.mp4&skin=SkinOverAllNoFullNoCaption.swf&skinAutoHide=true&skinBackgroundColor=0x5F5F5F&skinBackgroundAlpha=0.75&volume=1.00"
			 */
			String ^GetRichMediaItemPara(int idx)
			{
				return PDF_Page_getAnnotRichMediaItemPara(m_page->m_page, m_annot, idx);
			}
			/**
			 * get source of an item of RichMedia annotation.<br/>
			 * @param idx range in [0, Annot.GetRichMediaItemCount())
			 * @return parameter string, or null.<br/>
			 * example: "source=myvideo.mp4&skin=SkinOverAllNoFullNoCaption.swf&skinAutoHide=true&skinBackgroundColor=0x5F5F5F&skinBackgroundAlpha=0.75&volume=1.00"<br/>
			 * the source is "source=myvideo.mp4", return string is "myvideo.mp4"
			 */
			String ^GetRichMediaItemSource(int idx)
			{
				return PDF_Page_getAnnotRichMediaItemSource(m_page->m_page, m_annot, idx);
			}
			/**
			 * save source of an item of RichMedia annotation to a file.<br/>
			 * @param idx range in [0, Annot.GetRichMediaItemCount())
			 * @param save_path absolute path to save file, like "/sdcard/app_data/myvideo.mp4"
			 * @return true or false.
			 */
			Boolean GetRichMediaItemSourceData(int idx, String ^save_path)
			{
				return PDF_Page_getAnnotRichMediaItemSourceData(m_page->m_page, m_annot, idx, save_path);
			}
			/**
			 * save an asset to a file.<br/>
			 * @param asset asset name in RichMedia assets list.<br/>
			 * example:<br/>
			 * GetRichMediaItemAsset(0) return player window named as "VideoPlayer.swf"<br/>
			 * GetRichMediaItemPara(0) return "source=myvideo.mp4&skin=SkinOverAllNoFullNoCaption.swf&skinAutoHide=true&skinBackgroundColor=0x5F5F5F&skinBackgroundAlpha=0.75&volume=1.00".<br/>
			 * so we has 3 assets in item[0]:<br/>
			 * 1."VideoPlayer.swf"<br/>
			 * 2."myvideo.mp4"<br/>
			 * 3."SkinOverAllNoFullNoCaption.swf"
			 * @param save_path absolute path to save file, like "/sdcard/app_data/myvideo.mp4"
			 * @return true or false.
			 */
			Boolean GetRichMediaData(String ^name, String ^save_path)
			{
				return PDF_Page_getAnnotRichMediaData(m_page->m_page, m_annot, name, save_path);
			}
			/// <summary>
			/// Check if the annotation is a popup annotation
			/// </summary>
			property bool IsPopup
			{
				bool get()
				{
					String^ ret = PDF_Page_getAnnotPopupSubject(m_page->m_page, m_annot);
					return (ret != nullptr && !ret->IsEmpty());
				}
			}
			/// <summary>
			/// Get popup Annotation associate to this annotation.
			/// </summary>
			property PDFAnnot ^Popup
			{
				PDFAnnot ^get()
				{
					PDFAnnot ^ret = ref new PDFAnnot();
					ret->m_annot = PDF_Page_getAnnotPopup(m_page->m_page, m_annot);
					ret->m_page = m_page;
					return ret;
				}
			}
			/// <summary>
			/// Check if a popup annotaion is opened
			/// </summary>
			property Boolean PopupOpen
			{
				Boolean get()
				{
					return PDF_Page_getAnnotPopupOpen(m_page->m_page, m_annot);
				}
				void set(Boolean open)
				{
					PDF_Page_setAnnotPopupOpen(m_page->m_page, m_annot, open);
				}
			}
			property int ReplyCount
			{
				int get()
				{
					return PDF_Page_getAnnotReplyCount(m_page->m_page, m_annot);
				}
			}
			PDFAnnot^ GetReply(int idx)
			{
				PDF_ANNOT annot = PDF_Page_getAnnotReply(m_page->m_page, m_annot, idx);
				if (!annot) return nullptr;
				PDFAnnot^ ret = ref new PDFAnnot();
				ret->m_page = m_page;
				ret->m_annot = annot;
				return ret;
			}
			/// <summary>
			/// Get/Set subject to a popup annotation
			/// </summary>
			property String ^PopupSubject
			{
				String ^get()
				{
					return PDF_Page_getAnnotPopupSubject(m_page->m_page, m_annot);
				}
				void set( String ^txt )
				{
					PDF_Page_setAnnotPopupSubjectW( m_page->m_page, m_annot, txt->Data() );
				}
			}
			/// <summary>
			/// Get/Set content to a popup annotation
			/// </summary>
			property String ^PopupText
			{
				String ^get()
				{
					return PDF_Page_getAnnotPopupText(m_page->m_page, m_annot);
				}
				void set( String ^txt )
				{
					PDF_Page_setAnnotPopupTextW( m_page->m_page, m_annot, txt->Data() );
				}
			}
			/// <summary>
			/// /// Get/Set label to a popup annotation
			/// </summary>
			property String ^PopupLabel
			{
				String ^get()
				{
					return PDF_Page_getAnnotPopupLabel(m_page->m_page, m_annot);
				}
				void set(String ^txt)
				{
					PDF_Page_setAnnotPopupLabelW(m_page->m_page, m_annot, txt->Data());
				}
			}
			/**
			 * contents of edit-box.<br/>
			 */
			property String ^EditText
			{
				String ^get()
				{
					return PDF_Page_getAnnotEditText(m_page->m_page, m_annot);
				}
				void set( String ^txt )
				{
					bool ret = PDF_Page_setAnnotEditTextW( m_page->m_page, m_annot, txt->Data() );
					ret = 0;
				}
			}
			/**
			 * set font of edittext.<br/>
			 * you should re-render page to display modified data.<br/>
			 * this method require premium license.<br/>
			 * @param font DocFont object from Document.NewFontCID().
			 * @return true or false.
			 */
			bool SetEditFont(PDFDocFont ^font)
			{
				if (!font) return false;
				return PDF_Page_setAnnotEditFont(m_page->m_page, m_annot, font->m_font);
			}
			/**
			 * sign the empty field and save the PDF file.<br/>
			 * if the signature field is not empty(signed), it will return failed.<br/>
			 * this method require premium license.
			 * @param form appearance icon for signature
			 * @param cert_file a cert file like .p12 or .pfx file, DER encoded cert file.
			 * @param pswd password to open cert file.
			 * @param name signer name.
			 * @param reason sign reason will write to signature.
			 * @param location signature location will write to signature.
			 * @param contact contact info will write to signature.
			 * @return 0 mean OK<br/>
			 * -1: generate parameters error.<br/>
			 * -2: it is not signature field, or field has already signed.<br/>
			 * -3: invalid annotation data.<br/>
			 * -4: save PDF file failed.<br/>
			 * -5: cert file open failed.
			 */
			int SignField(PDFDocForm ^form, String ^cert_file, String ^pswd, String ^name, String ^reason, String ^location, String ^contact)
			{
				char *ccert_file = cvt_str_cstr(cert_file);
				char *cpswd = cvt_str_cstr(pswd);
				char *cname = cvt_str_cstr(name);
				char *creason = cvt_str_cstr(reason);
				char *clocation = cvt_str_cstr(location);
				char *ccontact = cvt_str_cstr(contact);
				return PDF_Page_signAnnotField(m_page->m_page, m_annot, form->m_form, ccert_file, cpswd, cname, creason, clocation, ccontact);
				free(ccontact);
				free(clocation);
				free(creason);
				free(cname);
				free(cpswd);
				free(ccert_file);
			}
			/**
			 * type of edit-box.
			 * -1: this annotation is not text-box.
			 * 1: normal single line.
			 * 2: password.
			 * 3: MultiLine edit area.
			 */
			property int EditType
			{
				int get(){return PDF_Page_getAnnotEditType(m_page->m_page, m_annot);}
			}
			/**
			 * position and size of edit-box.<br/>
			 * for FreeText annotation, position of edit-box is not the position of annotation.<br/>
			 */
			property RDRect EditTextRect
			{
				RDRect get()
				{
					RDRect rect;
					if( !PDF_Page_getAnnotEditTextRect( m_page->m_page, m_annot, (PDF_RECT *)&rect ) )
					{
						rect.left = 0;
						rect.top = 0;
						rect.right = 0;
						rect.bottom = 0;
					}
					return rect;
				}
			}
			/**
			 * text size of edit-box and edit field.<br/>
			 */
			property float EditTextSize
			{
				float get(){return PDF_Page_getAnnotEditTextSize(m_page->m_page, m_annot);}
				void set(float val) { PDF_Page_setAnnotEditTextSize(m_page->m_page, m_annot, val);}
			}
			/**
			 * text align of edit-box and edit field.<br/>
			 * 0: left, 1: center, 2: right.
			 */
			property int EditTextAlign
			{
				int get() { return PDF_Page_getAnnotEditTextAlign(m_page->m_page, m_annot); }
				void set(int val) { PDF_Page_setAnnotEditTextAlign(m_page->m_page, m_annot, val); }
			}
			/**
			 * text color of edit-box and edit field.<br/>
			 */
			property unsigned int EditTextColor
			{
				unsigned int get() {return PDF_Page_getAnnotEditTextColor(m_page->m_page, m_annot);}
				void set(unsigned int val) { PDF_Page_setAnnotEditTextColor(m_page->m_page, m_annot, val);}
			}
			/// <summary>
			/// Get item count of combo-box.
			/// Returns -1 if the annotation is not a combo-box.
			/// This method require premium license
			/// </summary>
			property int ComboItemCount
			{
				int get(){return PDF_Page_getAnnotComboItemCount(m_page->m_page, m_annot);}
			}
			/// <summary>
			/// Get/Set current selected item index of combo-box.
			/// Get() returns -1 if this is not combo-box or no item selected, otherwise the item index that selected.
			/// This method require premium license
			/// </summary>
			property int ComboItemSel
			{
				int get(){return PDF_Page_getAnnotComboItemSel(m_page->m_page, m_annot);}
				void set(int item){ PDF_Page_setAnnotComboItem(m_page->m_page, m_annot, item);}
			}
			/// <summary>
			/// Get export value of combo-box item.
			/// This method require premium license
			/// </summary>
			/// <param name="item">0 based item index. range:[0, GetAnnotComboItemCount()-1]</param>
			/// <returns>null if this is not combo-box or no item with specified index, otherwise the value of the item.</returns>
			String ^GetComboItem( int item )
			{
				return PDF_Page_getAnnotComboItem(m_page->m_page, m_annot, item);
			}
			/// <summary>
			/// Get item count of list-box.
			/// This method require premium license
			/// </summary>
			property int ListItemCount
			{
				int get(){return PDF_Page_getAnnotListItemCount(m_page->m_page, m_annot);}
			}
			/// <summary>
			/// Get/Set the index of selected list item
			/// This method require premium license
			/// </summary>
			property Array<int> ^ListItemSel
			{
				Array<int> ^get()
				{
					int sels[128];
					int cnt = PDF_Page_getAnnotListSels( m_page->m_page, m_annot, sels, 128 );
					Array<int> ^tmp = ref new Array<int>(cnt);
					memcpy( tmp->Data, sels, cnt * sizeof(int) );
					return tmp;
				}
				void set(const Array<int> ^sel)
				{
					PDF_Page_setAnnotListSels(m_page->m_page, m_annot, sel->Data, sel->Length);
				}
			}
			/// <summary>
			/// Get an item of list-box.
			/// This method require premium license
			/// </summary>0 based item index. range:[0, GetListItemCount()-1]
			/// <param name="item"></param>
			/// <returns></returns>
			String ^GetListItem( int item )
			{
				return PDF_Page_getAnnotListItem(m_page->m_page, m_annot, item);
			}

			/**
			 * status of check-box and radio-box.
			 * -1 if annotation is not valid control.
			 * 0 if check-box is not checked.
			 * 1 if check-box checked.
			 * 2 if radio-box is not checked.
			 * 3 if radio-box checked.
			 */
			int GetCheckStatus()
			{
				return PDF_Page_getAnnotCheckStatus( m_page->m_page, m_annot );
			}
			/**
			 * set value to check-box.<br/>
			 * you should render page again to display modified data.<br/>
			 * this method require premium license
			 * @param check true or false.
			 * @return true or false.
			 */
			Boolean SetCheckValue(Boolean check)
			{
				return PDF_Page_setAnnotCheckValue( m_page->m_page, m_annot, check );
			}
			/**
			 * check the radio-box and clear others in radio group.<br/>
			 * you should render page again to display modified data.<br/>
			 * this method require premium license
			 * @return true or false.
			 */
			Boolean SetRadio()
			{
				return PDF_Page_setAnnotRadio( m_page->m_page, m_annot );
			}
			/**
			 * stroke dash of square/circle/ink/line/ploygon/polyline/free text/text field annotation.<br/>
			 * for free text or text field annotation: dash of edit-box border<br/>
			 * you need render page again to show modified annotation.<br/>
			 * @param dash stroke dash units.
			 * @return true or false
			 */
			property Array<float> ^StrokeDash
			{
				Array<float>^ get() {
					float stmp[128];
					int cnt = PDF_Page_getAnnotStrokeDash(m_page->m_page, m_annot, stmp, 128);
					if (cnt <= 0) return nullptr;
					return ref new Array<float>(stmp, cnt);
				}
				void set(const Array<float>^ val)
				{
					if (val && val->Length > 0)
						PDF_Page_setAnnotStrokeDash(m_page->m_page, m_annot, val->Data, val->Length);
					else
						PDF_Page_setAnnotStrokeDash(m_page->m_page, m_annot, NULL, 0);
				}
			}
			/// <summary>
			/// Check if the annotation is a reset button
			/// </summary>
			/// <returns>True if it is a reset button, otherwise false</returns>
			Boolean IsResetButton()
			{
				return PDF_Page_getAnnotReset( m_page->m_page, m_annot );
			}
			/// <summary>
			/// Do annotation reset
			/// </summary>
			/// <returns>True is successed, otherwise false</returns>
			Boolean DoReset()
			{
				return PDF_Page_setAnnotReset( m_page->m_page, m_annot );
			}
			/// <summary>
			/// Get annotation submit target on a submit button.
			/// This method require premium license
			/// </summary>
			property String ^SubmitTarget
			{
				String ^get()
				{
					return PDF_Page_getAnnotSubmitTarget(m_page->m_page, m_annot);
				}
			}
			/// <summary>
			/// Get annotation submit parameters on a submit button.
			/// Mail mode : return whole XML string for form data.
			/// Other mode : url data likes : "para1=xxx&para2=xxx".
			/// This method require premium license
			/// </summary>
			property String ^SubmitPara
			{
				String ^get()
				{
					wchar_t uri[512];
					if( !PDF_Page_getAnnotSubmitParaW( m_page->m_page, m_annot, uri, 511 ) ) return nullptr;
					else return ref new String(uri);
				}
			}
			/// <summary>
			/// Get status of signature field. 
			/// Returns:
			/// -1 if this is not signature field
			/// 0 if not signed.
			/// 1 if signed.
			/// This method require premium license
			/// </summary>
			property int SignStatus
			{
				int get()
				{
					return PDF_Page_getAnnotSignStatus(m_page->m_page, m_annot);
				}
			}
			/// <summary>
			/// Get PDFSign object embedded
			/// </summary>
			property PDFSign ^Sign
			{
				PDFSign ^get()
				{
					PDF_SIGN sign = PDF_Page_getAnnotSign(m_page->m_page, m_annot);
					if (!sign) return nullptr;
					PDFSign ^ret = ref new PDFSign;
					ret->m_sign = sign;
					return ret;
				}
			}
			/// <summary>
			/// Get/Set a PDFObj object which is used to lock the PDFSign object
			/// </summary>
			property PDFObj^ SignLock
			{
				PDFObj^ get()
				{
					PDF_OBJ obj = PDF_Page_getAnnotSignLock(m_page->m_page, m_annot);
					if (!obj) return nullptr;
					return ref new PDFObj(obj);
				}
				void set(PDFObj ^obj)
				{
					if (obj) PDF_Page_setAnnotSignLock(m_page->m_page, m_annot, obj->m_obj);
				}
			}
			/// <summary>
			/// Get annotation reference
			/// </summary>
			property PDFRef Ref
			{
				PDFRef get()
				{
					PDFRef ref;
					ref.ref = PDF_Page_getAnnotRef(m_page->m_page, m_annot);
					return ref;
				}
			}
			/// <summary>
			/// Get advanced annotation reference
			/// </summary>
			/// <returns></returns>
			PDFRef Advance_GetRef()
			{
				PDF_OBJ_REF ref = PDF_Page_advGetAnnotRef(m_page->m_page, m_annot);
				PDFRef ret;
				ret.ref = ref;
				return ret;
			}
			/// <summary>
			/// Advanced function to reload page object, after advanced methods update Page object data.
			/// All annotations return from Page.GetAnnot() or Page.GetAnnotFromPoint() shall not available.after this method invoked.
			/// This method require premium license.
			/// </summary>
			void Advance_Reload()
			{
				PDF_Page_advReloadAnnot(m_page->m_page, m_annot);
			}
			/**
			 * export data from annotation.<br/>
			 * @return a byte array saved annotation data.
			 */
			Array<BYTE> ^Export()
			{
				unsigned char *buf = (unsigned char *)malloc(8192);
				int len = PDF_Page_exportAnnot(m_page->m_page, m_annot, buf, 8192);
				Array<BYTE> ^ret = ref new Array<BYTE>(len);
				memcpy(ret->Data, buf, len);
				free(buf);
				return ret;
			}
			/// <summary>
			/// Get point from line annotation.
			/// This method require professional or premium license
			/// </summary>
			/// <param name="index">0: start point, others: end point.</param>
			/// <returns>The target point</returns>
			RDPoint GetLinePoint(int index)
			{
				PDF_POINT pt;
				pt.x = 0;
				pt.y = 0;
				PDF_Page_getAnnotLinePoint(m_page->m_page, m_annot, index, &pt);
				return *(RDPoint*)&pt;
			}
			/// <summary>
			/// Set the start and end point of a line annotation
			/// </summary>
			/// <param name="x1">x coordinate of start point</param>
			/// <param name="y1">y coordinate of end point</param>
			/// <param name="x2">x coordinate of start point</param>
			/// <param name="y2">y coordinate of end point</param>
			/// <returns></returns>
			bool SetLinePoint(float x1, float y1, float x2, float y2)
			{
				return PDF_Page_setAnnotLinePoint(m_page->m_page, m_annot, x1, y1, x2, y2);
			}
		private:
			PDFAnnot()
			{
				m_page = nullptr;
				m_annot = NULL;
			}
			friend PDFPage;
			PDFPage ^m_page;
			PDF_ANNOT m_annot;
		};
	}
}

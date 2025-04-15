#pragma once
#include <windows.h>
#include <collection.h>
#include <ppltasks.h>
using namespace Platform;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace Windows::Graphics::Imaging;

#ifdef __cplusplus
class IPDFStream
{
public:
	virtual bool Writeable() const = 0;
	virtual unsigned long long GetLen() const = 0;
	virtual unsigned long long GetPos() const = 0;
	virtual bool SetPos( unsigned long long off ) = 0;
	virtual unsigned int Read( void *pBuf, unsigned int dwBuf ) = 0;
	virtual unsigned int Write( const void *pBuf, unsigned int dwBuf ) = 0;
	virtual void Close() = 0;
	virtual void Flush() = 0;
};

class IPDFJSDelegate
{
public:
	virtual void OnConsole(int cmd, const char *para) = 0;
	virtual int OnAlert(int btn, const char *msg, const char *title) = 0;
	virtual bool OnDocClose() = 0;
	virtual char *OnTmpFile() = 0;
	virtual void OnUncaughtException(int code, const char *msg) = 0;
};

extern "C" {
#endif
typedef enum
{
	err_ok,
	err_invalid_para,
    err_open,
    err_password,
    err_encrypt,
    err_bad_file,
}PDF_ERR;
typedef enum
{
    mode_poor = 0,
    mode_normal = 1,
    mode_best = 2,
}PDF_RENDER_MODE;
typedef struct
{
    float x;
    float y;
}PDF_POINT;
typedef struct
{
    float left;
    float top;
    float right;
    float bottom;
}PDF_RECT;

typedef struct _PDFDIB* PDF_DIB;
typedef struct _PDF_BMP* PDF_BMP;
typedef struct _PDF_MATRIX* PDF_MATRIX;
typedef struct _PDF_PATH* PDF_PATH;
typedef struct _PDF_INK* PDF_INK;
typedef struct _PDF_DOC* PDF_DOC;

String^ Global_getVersion();
int Global_active(String^ serial);
//load starnard font from path
void Global_loadStdFont(int index, const char* path);
//reserved
bool Global_SaveFont(const char* fname, const char* save_file);
//unload standard font
void Global_unloadStdFont(int index);
//set cmaps path
void Global_setCMapsPath(const char* cmaps, const char* umaps);
bool Global_setCMYKICC(const char* path);
//create font list
void Global_fontfileListStart();
//add a true type font file to font list
void Global_fontfileListAdd(const char* font_file);
//end font list
void Global_fontfileListEnd();
bool Global_fontfileMapping(const char* map_name, const char* name);
//set default font
bool Global_setDefaultFont(const char* collection, const char* font_name, bool fixed);
//set annotation font for editing mostly for edit-box/combo-box
bool Global_setAnnotFont(const char* font_name);
int Global_getFaceCount();
const char* Global_getFaceName(int index);
//create a DIB object
PDF_DIB Global_dibGet(PDF_DIB dib, int width, int height);
//free DIB object.
void Global_dibFree(PDF_DIB dib);
void Global_toDIBPoint(PDF_MATRIX matrix, const PDF_POINT* ppoint, PDF_POINT* dpoint);
void Global_toPDFPoint(PDF_MATRIX matrix, const PDF_POINT* dpoint, PDF_POINT* ppoint);
void Global_toDIBRect(PDF_MATRIX matrix, const PDF_RECT* prect, PDF_RECT* drect);
void Global_toPDFRect(PDF_MATRIX matrix, const PDF_RECT* drect, PDF_RECT* prect);
//get pixels data from DIB object
void* Global_dibGetData(PDF_DIB dib);
int Global_dibGetWidth(PDF_DIB dib);
int Global_dibGetHeight(PDF_DIB dib);
bool Global_dibSaveJPG(PDF_DIB dib, const char* path, int quality);
void Global_drawScroll(WriteableBitmap^ dst, PDF_DIB dib1, PDF_DIB dib2, int x, int y, int style, unsigned int back_side_clr);
bool Global_drawAnnotIcon(int annot_type, int icon, WriteableBitmap^ dib);
bool Global_drawDashLine(const float* dash, int dash_cnt, WriteableBitmap^ dib);
bool Global_drawLineHead(int head, WriteableBitmap^ dib);
PDF_BMP Global_lockBitmap(WriteableBitmap^ dst);
PDF_BMP Global_lockSoftBitmap(SoftwareBitmap^ dst);
bool Global_saveBitmapJPG(PDF_BMP data, const char* path, int quality);
void Global_unlockBitmap(PDF_BMP data);
void Global_scaleDIB(PDF_DIB dst, PDF_DIB dib);
void Global_drawDIB(PDF_BMP dst, PDF_DIB dib, int x, int y);
void Global_drawRect(PDF_BMP dst, int color, int x, int y, int w, int h, int mode);
void Global_eraseColor(PDF_BMP dst, int color);
void Global_setAnnotTransparency(int color);

//create a matrix object
PDF_MATRIX Matrix_create(float xx, float yx, float xy, float yy, float x0, float y0);
PDF_MATRIX Matrix_createScale(float scalex, float scaley, float x0, float y0);
void Matrix_invert(PDF_MATRIX matrix);
void Matrix_transformPath(PDF_MATRIX matrix, PDF_PATH path);
void Matrix_transformInk(PDF_MATRIX matrix, PDF_INK ink);
void Matrix_transformRect(PDF_MATRIX matrix, PDF_RECT* rect);
void Matrix_transformPoint(PDF_MATRIX matrix, PDF_POINT* point);
void Matrix_destroy(PDF_MATRIX matrix);

//create a Ink object
PDF_INK Ink_create(float line_w, int color);
void Ink_destroy(PDF_INK ink);
void Ink_onDown(PDF_INK ink, float x, float y);
void Ink_onMove(PDF_INK ink, float x, float y);
void Ink_onUp(PDF_INK ink, float x, float y);
int Ink_getNodeCount(PDF_INK ink);
int Ink_getNode(PDF_INK hand, int index, PDF_POINT* pt);

PDF_PATH Path_create();
void Path_moveTo(PDF_PATH path, float x, float y);
void Path_lineTo(PDF_PATH path, float x, float y);
void Path_curveTo(PDF_PATH path, float x1, float y1, float x2, float y2, float x3, float y3);
void Path_closePath(PDF_PATH path);
void Path_destroy(PDF_PATH path);
int Path_getNodeCount(PDF_PATH path);
int Path_getNode(PDF_PATH path, int index, PDF_POINT* pt);

#ifdef __cplusplus
}
#endif

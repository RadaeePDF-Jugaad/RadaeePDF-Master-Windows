#pragma once
#include <Windows.h>
#include "RDCom.h"
using namespace RDDLib::comm;
using namespace Windows::Foundation;
using namespace Windows::UI::Xaml;
namespace RDDLib
{
	namespace reader
	{
		public interface class IContentListener
		{
			void cFillRect(const RDRect& rect, Windows::UI::Color clr);//to fill rectangles
			void cSetPos(double vx, double vy);//to set scroll position.
			void cAttachBmp(WriteableBitmap^ bmp, const Array<byte>^ arr);
			void cDetachBmp(WriteableBitmap^ bmp);
			void cFound(bool found);
		};
		public interface class IThumbListener
		{
			void OnPageSelected(int pageno);
		};
		public enum class RDLAYOUT_MODE
		{
			layout_unknown = 0,
			layout_vert,
			layout_horz,
			layout_dual,
			layout_dual_cover,
			layout_hsingle,
			layout_hdual,
		};
	}
}

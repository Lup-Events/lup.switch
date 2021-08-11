using System;
using OpenCvSharp;

namespace Lup.TwilioSwitch
{
    public static class MatExtensions
    {
        public static void PutTextShadow(this Mat target, String text, Scalar color, Int32 x, Int32 y, Int32 size)
        {
            if (null == target)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            
            target.PutText(text, new Point(x, y), HersheyFonts.HersheyPlain, size, Scalar.White, 12, LineTypes.Link8, false);
            target.PutText(text, new Point(x, y), HersheyFonts.HersheyPlain, size, color, size * 2, LineTypes.Link8, false);
        }
    }
}
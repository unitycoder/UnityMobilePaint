using UnityEngine;
using System.Collections;

namespace unitycoder_MobilePaint
{

	public static class PaintTools
	{

		// clamp int value, http://stackoverflow.com/b/3040551
		public static int ClampBrushInt(int value, int brushSize, int max)
		{
			return (value < brushSize) ? brushSize : (value > max) ? max : value;
		}

		/*
		public static int CropBrushInt(int value, int brushSize, int max)
		{
			return (value < brushSize) ? brushSize : (value > max) ? max : value;
		}*/

		
		// modified from http://pastebin.com/7wnvR4se
		public static byte LerpByte(byte from, int to, int value)
		{
			
			if (from < to)
			{
				
				if (value < from)
				{
					return 0;
				}
				else if (value > to)
				{
					return 255;
				}
				else
				{
					return (byte)((value - from) / (to - from));
				}
			}
			else
			{
				if (from <= to)
				{
					return 0;
				}
				else if (value < to)
				{
					return 255;
				}
				else if (value > from)
				{
					return 0;
				}
				else
				{
					return (byte)(255 - (value - to) / (from - to));
				}
			}
		} // LerpByte

		// http://stackoverflow.com/b/8070071
		// FastDivide255: pt->r = (r+1 + (r >> 8)) >> 8; // fast way to divide by 255

	}
}
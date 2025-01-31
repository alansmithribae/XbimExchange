﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xbim.Common.Logging;
using Xbim.COBieLiteUK;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Globalization;

namespace Xbim.CobieLiteUK.Validation.Reporting
{
    internal class ExcelCellVisualValue
    {
        public ExcelCellVisualValue()
        { }

        public ExcelCellVisualValue(ExcelWorksheet worksheet)
        {
        }

        /// <summary>
        /// Sets cell value and style based on IVisualValue
        /// </summary>
        /// <param name="excelCell">Cell to apply value and style to</param>
        /// <param name="visualValue"></param>
        internal void SetCell(ExcelRange excelCell, IVisualValue visualValue)
        {
            if (visualValue.AttentionStyle == VisualAttentionStyle.None)
            {
                //excelCell.CellStyle = _neutral;
            }
            switch (visualValue.AttentionStyle)
            {
                case VisualAttentionStyle.Amber:
                    excelCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    excelCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 204));
                    break;
                case VisualAttentionStyle.Green:
                    excelCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    excelCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 239, 206));
                    break; ;
                case VisualAttentionStyle.Red:
                    excelCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    excelCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 183, 185));
                    break;
            }

            var attribute = visualValue.VisualValue;
            if (attribute is StringAttributeValue)
            {
                excelCell.Value = ((StringAttributeValue)(attribute)).Value;
            }
            else if (attribute is IntegerAttributeValue)
            {
                var v = ((IntegerAttributeValue)(attribute)).Value;
                if (v.HasValue)
                {
                    excelCell.Value = (double)v.Value;
                }
            }
            else if (attribute is DecimalAttributeValue)
            {
                var v = ((DecimalAttributeValue)(attribute)).Value;
                if (v.HasValue)
                {
                    excelCell.Value = (double)v.Value;
                }
            }
            else if (attribute is BooleanAttributeValue)
            {
                var v = ((BooleanAttributeValue)(attribute)).Value;
                if (v.HasValue)
                {
                    excelCell.Value = v.Value;
                }
            }
            else if (attribute is DateTimeAttributeValue)
            {
                var v = ((DateTimeAttributeValue)(attribute)).Value;
                if (!v.HasValue)
                    return;
                excelCell.Value = v.Value.ToLongDateString();
                excelCell.Value = v.Value.ToString("G", DateTimeFormatInfo.InvariantInfo);
            }
        }
    }
}


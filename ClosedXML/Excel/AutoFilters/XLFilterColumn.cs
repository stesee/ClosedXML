using System;
using System.Linq;
using System.Collections.Generic;

namespace ClosedXML.Excel
{
    internal class XLFilterColumn : IXLFilterColumn
    {
        private readonly XLAutoFilter _autoFilter;
        private readonly int _column;

        public XLFilterColumn(XLAutoFilter autoFilter, int column)
        {
            _autoFilter = autoFilter;
            _column = column;
        }

        #region IXLFilterColumn Members

        public void Clear()
        {
            if (_autoFilter.Filters.ContainsKey(_column))
                _autoFilter.Filters.Remove(_column);
        }

        public IXLFilteredColumn AddFilter<T>(T value) where T : IComparable<T>
        {
            if (typeof(T) == typeof(string))
            {
                ApplyCustomFilter(value, XLFilterOperator.Equal,
                                  v =>
                                  v.ToString().Equals(value.ToString(), StringComparison.InvariantCultureIgnoreCase),
                                  XLFilterType.Regular);
            }
            else
            {
                ApplyCustomFilter(value, XLFilterOperator.Equal,
                                  v => v.CastTo<T>().CompareTo(value) == 0, XLFilterType.Regular);
            }
            return new XLFilteredColumn(_autoFilter, _column);
        }

        public IXLDateTimeGroupFilteredColumn AddDateGroupFilter(DateTime date, XLDateTimeGrouping dateTimeGrouping)
        {
            bool condition(object date2) => XLDateTimeGroupFilteredColumn.IsMatch(date, (DateTime)date2, dateTimeGrouping);

            _autoFilter.IsEnabled = true;

            if (_autoFilter.Filters.TryGetValue(_column, out var filterList))
                filterList.Add(
                    new XLFilter
                    {
                        Value = date,
                        Operator = XLFilterOperator.Equal,
                        Connector = XLConnector.Or,
                        Condition = condition,
                        DateTimeGrouping = dateTimeGrouping
                    }
                );
            else
            {
                _autoFilter.Filters.Add(
                    _column,
                    new List<XLFilter>
                    {
                        new XLFilter
                        {
                            Value = date,
                            Operator = XLFilterOperator.Equal,
                            Connector = XLConnector.Or,
                            Condition = condition,
                            DateTimeGrouping = dateTimeGrouping
                        }
                    }
                );
            }

            _autoFilter.Column(_column).FilterType = XLFilterType.DateTimeGrouping;

            var ws = _autoFilter.Range.Worksheet as XLWorksheet;
            ws.SuspendEvents();

            var rows = _autoFilter.Range.Rows(2, _autoFilter.Range.RowCount());

            foreach (var row in rows)
            {
                if (row.Cell(_column).DataType == XLDataType.DateTime && condition(row.Cell(_column).GetDateTime()))
                    row.WorksheetRow().Unhide();
                else
                    row.WorksheetRow().Hide();
            }
            ws.ResumeEvents();

            return new XLDateTimeGroupFilteredColumn(_autoFilter, _column);
        }

        public void Top(int value, XLTopBottomType type = XLTopBottomType.Items)
        {
            _autoFilter.Column(_column).TopBottomPart = XLTopBottomPart.Top;
            SetTopBottom(value, type);
        }

        public void Bottom(int value, XLTopBottomType type = XLTopBottomType.Items)
        {
            _autoFilter.Column(_column).TopBottomPart = XLTopBottomPart.Bottom;
            SetTopBottom(value, type, false);
        }

        public void AboveAverage()
        {
            ShowAverage(true);
        }

        public void BelowAverage()
        {
            ShowAverage(false);
        }

        public IXLFilterConnector EqualTo<T>(T value) where T : IComparable<T>
        {
            if (typeof(T) == typeof(string))
            {
                return ApplyCustomFilter(value, XLFilterOperator.Equal,
                                         v =>
                                         v.ToString().Equals(value.ToString(),
                                                             StringComparison.InvariantCultureIgnoreCase));
            }

            return ApplyCustomFilter(value, XLFilterOperator.Equal,
                                     v => v.CastTo<T>().CompareTo(value) == 0);
        }

        public IXLFilterConnector NotEqualTo<T>(T value) where T : IComparable<T>
        {
            if (typeof(T) == typeof(string))
            {
                return ApplyCustomFilter(value, XLFilterOperator.NotEqual,
                                         v =>
                                         !v.ToString().Equals(value.ToString(),
                                                              StringComparison.InvariantCultureIgnoreCase));
            }

            return ApplyCustomFilter(value, XLFilterOperator.NotEqual,
                                        v => v.CastTo<T>().CompareTo(value) != 0);
        }

        public IXLFilterConnector GreaterThan<T>(T value) where T : IComparable<T>
        {
            return ApplyCustomFilter(value, XLFilterOperator.GreaterThan,
                                     v => v.CastTo<T>().CompareTo(value) > 0);
        }

        public IXLFilterConnector LessThan<T>(T value) where T : IComparable<T>
        {
            return ApplyCustomFilter(value, XLFilterOperator.LessThan,
                                     v => v.CastTo<T>().CompareTo(value) < 0);
        }

        public IXLFilterConnector EqualOrGreaterThan<T>(T value) where T : IComparable<T>
        {
            return ApplyCustomFilter(value, XLFilterOperator.EqualOrGreaterThan,
                                     v => v.CastTo<T>().CompareTo(value) >= 0);
        }

        public IXLFilterConnector EqualOrLessThan<T>(T value) where T : IComparable<T>
        {
            return ApplyCustomFilter(value, XLFilterOperator.EqualOrLessThan,
                                     v => v.CastTo<T>().CompareTo(value) <= 0);
        }

        public void Between<T>(T minValue, T maxValue) where T : IComparable<T>
        {
            EqualOrGreaterThan(minValue).And.EqualOrLessThan(maxValue);
        }

        public void NotBetween<T>(T minValue, T maxValue) where T : IComparable<T>
        {
            LessThan(minValue).Or.GreaterThan(maxValue);
        }

        public static Func<string, object, bool> BeginsWithFunction { get; } = (value, input) => ((string)input).StartsWith(value, StringComparison.InvariantCultureIgnoreCase);

        public IXLFilterConnector BeginsWith(string value)
        {
            return ApplyCustomFilter(value + "*", XLFilterOperator.Equal, s => BeginsWithFunction(value, s));
        }

        public IXLFilterConnector NotBeginsWith(string value)
        {
            return ApplyCustomFilter(value + "*", XLFilterOperator.NotEqual, s => !BeginsWithFunction(value, s));
        }

        public static Func<string, object, bool> EndsWithFunction { get; } = (value, input) => ((string)input).EndsWith(value, StringComparison.InvariantCultureIgnoreCase);

        public IXLFilterConnector EndsWith(string value)
        {
            return ApplyCustomFilter("*" + value, XLFilterOperator.Equal, s => EndsWithFunction(value, s));
        }

        public IXLFilterConnector NotEndsWith(string value)
        {
            return ApplyCustomFilter("*" + value, XLFilterOperator.NotEqual, s => !EndsWithFunction(value, s));
        }

        public static Func<string, object, bool> ContainsFunction { get; } = (value, input) => ((string)input).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

        public IXLFilterConnector Contains(string value)
        {
            return ApplyCustomFilter("*" + value + "*", XLFilterOperator.Equal, s => ContainsFunction(value, s));
        }

        public IXLFilterConnector NotContains(string value)
        {
            return ApplyCustomFilter("*" + value + "*", XLFilterOperator.Equal, s => !ContainsFunction(value, s));
        }

        public XLFilterType FilterType { get; set; }

        public int TopBottomValue { get; set; }
        public XLTopBottomType TopBottomType { get; set; }
        public XLTopBottomPart TopBottomPart { get; set; }

        public XLFilterDynamicType DynamicType { get; set; }
        public double DynamicValue { get; set; }

        #endregion IXLFilterColumn Members

        private void SetTopBottom(int value, XLTopBottomType type, bool takeTop = true)
        {
            _autoFilter.IsEnabled = true;
            _autoFilter.Column(_column).SetFilterType(XLFilterType.TopBottom)
                                       .SetTopBottomValue(value)
                                       .SetTopBottomType(type);

            var values = GetValues(value, type, takeTop);

            Clear();
            _autoFilter.Filters.Add(_column, new List<XLFilter>());

            var addToList = true;
            var ws = _autoFilter.Range.Worksheet as XLWorksheet;
            ws.SuspendEvents();
            var rows = _autoFilter.Range.Rows(2, _autoFilter.Range.RowCount());
            foreach (var row in rows)
            {
                var foundOne = false;
                foreach (var val in values)
                {
                    bool condition(object v) => (v as IComparable).CompareTo(val) == 0;
                    if (addToList)
                    {
                        _autoFilter.Filters[_column].Add(new XLFilter
                        {
                            Value = val,
                            Operator = XLFilterOperator.Equal,
                            Connector = XLConnector.Or,
                            Condition = condition
                        });
                    }

                    var cell = row.Cell(_column);
                    if (cell.DataType != XLDataType.Number || !condition(cell.GetDouble())) continue;
                    row.WorksheetRow().Unhide();
                    foundOne = true;
                }
                if (!foundOne)
                    row.WorksheetRow().Hide();

                addToList = false;
            }
            ws.ResumeEvents();
        }

        private IEnumerable<double> GetValues(int value, XLTopBottomType type, bool takeTop)
        {
            var column = _autoFilter.Range.Column(_column);
            var subColumn = column.Column(2, column.CellCount());
            var cellsUsed = subColumn.CellsUsed(c => c.DataType == XLDataType.Number);
            if (takeTop)
            {
                if (type == XLTopBottomType.Items)
                {
                    return cellsUsed.Select(c => c.GetDouble()).OrderByDescending(d => d).Take(value).Distinct();
                }

                var numerics1 = cellsUsed.Select(c => c.GetDouble());
                var valsToTake1 = numerics1.Count() * value / 100;
                return numerics1.OrderByDescending(d => d).Take(valsToTake1).Distinct();
            }

            if (type == XLTopBottomType.Items)
            {
                return cellsUsed.Select(c => c.GetDouble()).OrderBy(d => d).Take(value).Distinct();
            }

            var numerics = cellsUsed.Select(c => c.GetDouble());
            var valsToTake = numerics.Count() * value / 100;
            return numerics.OrderBy(d => d).Take(valsToTake).Distinct();
        }

        private void ShowAverage(bool aboveAverage)
        {
            _autoFilter.IsEnabled = true;
            _autoFilter.Column(_column).SetFilterType(XLFilterType.Dynamic)
                .SetDynamicType(aboveAverage
                                    ? XLFilterDynamicType.AboveAverage
                                    : XLFilterDynamicType.BelowAverage);
            var values = GetAverageValues(aboveAverage);

            Clear();
            _autoFilter.Filters.Add(_column, new List<XLFilter>());

            var addToList = true;
            var ws = _autoFilter.Range.Worksheet as XLWorksheet;
            ws.SuspendEvents();
            var rows = _autoFilter.Range.Rows(2, _autoFilter.Range.RowCount());

            foreach (var row in rows)
            {
                var foundOne = false;
                foreach (var val in values)
                {
                    bool condition(object v) => (v as IComparable).CompareTo(val) == 0;
                    if (addToList)
                    {
                        _autoFilter.Filters[_column].Add(new XLFilter
                        {
                            Value = val,
                            Operator = XLFilterOperator.Equal,
                            Connector = XLConnector.Or,
                            Condition = condition
                        });
                    }

                    var cell = row.Cell(_column);
                    if (cell.DataType != XLDataType.Number || !condition(cell.GetDouble())) continue;
                    row.WorksheetRow().Unhide();
                    foundOne = true;
                }

                if (!foundOne)
                    row.WorksheetRow().Hide();

                addToList = false;
            }

            ws.ResumeEvents();
        }

        private IEnumerable<double> GetAverageValues(bool aboveAverage)
        {
            var column = _autoFilter.Range.Column(_column);
            var subColumn = column.Column(2, column.CellCount());
            var average = subColumn.CellsUsed(c => c.DataType == XLDataType.Number).Select(c => c.GetDouble())
                .Average();

            if (aboveAverage)
            {
                return
                    subColumn.CellsUsed(c => c.DataType == XLDataType.Number).Select(c => c.GetDouble())
                        .Where(c => c > average).Distinct();
            }

            return
                subColumn.CellsUsed(c => c.DataType == XLDataType.Number).Select(c => c.GetDouble())
                    .Where(c => c < average).Distinct();
        }

        private IXLFilterConnector ApplyCustomFilter<T>(T value, XLFilterOperator op, Func<object, bool> condition,
                                                        XLFilterType filterType = XLFilterType.Custom)
            where T : IComparable<T>
        {
            _autoFilter.IsEnabled = true;
            if (filterType == XLFilterType.Custom)
            {
                Clear();
                _autoFilter.Filters.Add(_column,
                                        new List<XLFilter>
                                            {
                                                new XLFilter
                                                {
                                                    Value = value,
                                                    Operator = op,
                                                    Connector = XLConnector.Or,
                                                    Condition = condition
                                                }
                                            });
            }
            else
            {
                if (_autoFilter.Filters.TryGetValue(_column, out var filterList))
                    filterList.Add(new XLFilter
                    {
                        Value = value,
                        Operator = op,
                        Connector = XLConnector.Or,
                        Condition = condition
                    });
                else
                {
                    _autoFilter.Filters.Add(_column,
                                            new List<XLFilter>
                                                {
                                                    new XLFilter
                                                        {
                                                            Value = value,
                                                            Operator = op,
                                                            Connector = XLConnector.Or,
                                                            Condition = condition
                                                        }
                                                });
                }
            }
            _autoFilter.Column(_column).FilterType = filterType;
            _autoFilter.Reapply();
            return new XLFilterConnector(_autoFilter, _column);
        }

        public IXLFilterColumn SetFilterType(XLFilterType value) { FilterType = value; return this; }

        public IXLFilterColumn SetTopBottomValue(int value) { TopBottomValue = value; return this; }

        public IXLFilterColumn SetTopBottomType(XLTopBottomType value) { TopBottomType = value; return this; }

        public IXLFilterColumn SetTopBottomPart(XLTopBottomPart value) { TopBottomPart = value; return this; }

        public IXLFilterColumn SetDynamicType(XLFilterDynamicType value) { DynamicType = value; return this; }

        public IXLFilterColumn SetDynamicValue(double value) { DynamicValue = value; return this; }
    }
}

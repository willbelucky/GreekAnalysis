using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GreekAnalysis.Curves;

namespace GreekAnalysis.Products
{
    public class Bond
    {
        private DateTime IssueDate { get; }
        private DateTime MaturityDate { get; }
        private double CouponRate { get; }
        private int CouponFrequency { get; }
        private bool IsForward { get;}
        private double? price = null;
        private MarketCurve MarketCurve { get; }

        public Bond(DateTime issueDate, DateTime maturityDate, double couponRate, int couponFrequency, bool isForward, SortedDictionary<double, double> marketCurve)
        {
            IssueDate = issueDate;
            MaturityDate = maturityDate;
            CouponRate = couponRate;
            CouponFrequency = couponFrequency;
            IsForward = isForward;
            MarketCurve = new MarketCurve(marketCurve, couponFrequency); ;
        }
        
        public int CouponGapMonth()
        {
            return 12 / CouponFrequency;
        }

        public double Price(DateTime evaluationDate)
        {
            if(!price.HasValue)
            {
                price = Price(evaluationDate, MarketCurve);
            }

            return price.Value;
        }

        public double Price(DateTime evaluationDate, MarketCurve marketCurve)
        {

            // Cashflow table
            SortedDictionary<double, double> cashflows = GetCashflows(evaluationDate);
            if (cashflows.Count == 1) return 0;

            // Build zero coupon curve
            ZeroCouponCurve zeroCouponCurve = marketCurve.BuildZeroCouponCurve();

            // Discount
            double price = 0;
            foreach (KeyValuePair<double, double> pair in cashflows)
            {
                double yearFrac = pair.Key;
                double cashflow = pair.Value;
                price += cashflow * zeroCouponCurve.DiscountFactor(yearFrac);
            }

            return price;
        }

        private SortedDictionary<double, double> GetCashflows(DateTime evaluationDate)
        {
            SortedDictionary<double, double> cashflows = new SortedDictionary<double, double>();
            double yearFrac;
            if (IsForward)
            {
                DateTime prevCashflowDate = IssueDate;
                DateTime cashflowDate = IssueDate.AddMonths(CouponGapMonth());
                while (cashflowDate < MaturityDate)
                {
                    yearFrac = (cashflowDate - evaluationDate).TotalDays / 365;
                    if (yearFrac > 0)
                    {
                        cashflows.Add(yearFrac, CouponRate * (cashflowDate - prevCashflowDate).TotalDays / 365);
                    }
                    prevCashflowDate = cashflowDate;
                    cashflowDate = cashflowDate.AddMonths(CouponGapMonth());
                }

                yearFrac = (MaturityDate - evaluationDate).TotalDays / 365;
                if (yearFrac > 0)
                {
                    cashflows.Add((MaturityDate - evaluationDate).TotalDays / 365, CouponRate * (MaturityDate - prevCashflowDate).TotalDays / 365);
                }
            }
            else
            {
                DateTime prevCashflowDate = MaturityDate.AddMonths(-CouponGapMonth());
                DateTime cashflowDate = MaturityDate;
                while (prevCashflowDate > IssueDate)
                {
                    yearFrac = (cashflowDate - evaluationDate).TotalDays / 365;
                    if (yearFrac > 0)
                    {
                        cashflows.Add(yearFrac, CouponRate * (cashflowDate - prevCashflowDate).TotalDays / 365);
                    }
                    cashflowDate = prevCashflowDate;
                    prevCashflowDate = prevCashflowDate.AddMonths(-CouponGapMonth());
                }

                yearFrac = (IssueDate - evaluationDate).TotalDays / 365;
                if (yearFrac > 0)
                {
                    cashflows.Add((IssueDate - evaluationDate).TotalDays / 365, CouponRate * (IssueDate - cashflowDate).TotalDays / 365);
                }
            }

            // Add principal
            if (cashflows.Count != 0)
            {
                double lastYearFrac = cashflows.Keys.Max();
                cashflows[lastYearFrac] += 1;
            }

            return cashflows;
        }

        public double Delta(DateTime evaluationDate, double tenor)
        {
            SortedDictionary<double, double> marketCurve = MarketCurve.ToSortedDictionary();
            if(!marketCurve.ContainsKey(tenor))
            {
                throw new ArgumentException("The tenor is not exist.");
            }

            SortedDictionary<double, double> downDeltaCurve = new SortedDictionary<double, double>(marketCurve.ToDictionary(entry => entry.Key, entry => entry.Value));
            downDeltaCurve[tenor] -= 0.0001;
            double downDeltaPrice = Price(evaluationDate, new MarketCurve(downDeltaCurve, CouponFrequency));

            SortedDictionary<double, double> upDeltaCurve = new SortedDictionary<double, double>(marketCurve.ToDictionary(entry => entry.Key, entry => entry.Value));
            upDeltaCurve[tenor] += 0.0001;
            double upDeltaPrice = Price(evaluationDate, new MarketCurve(upDeltaCurve, CouponFrequency));

            double delta = (downDeltaPrice - upDeltaPrice) / 2;

            return delta;
        }
    }
}

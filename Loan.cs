﻿using System;
using System.Collections.Generic;

namespace LoanRepaymentProjector
{
    public class Loan
    {
        private static int _idGenerator = 0;

        public readonly int Id;

        /// <summary>
        /// The amount of interest accrued on the loan
        /// </summary>
        public decimal AccruedInterest { get; private set; }

        /// <summary>
        /// Loan's interest rate
        /// </summary>
        public decimal InterestRate { get; set; }

        /// <summary>
        /// The principal balance of the loan
        /// </summary>
        public decimal Principal { get; private set; }

        /// <summary>
        /// The Date for which the Loan's principal balance applies.
        /// </summary>
        public DateTime PrincipalEffectiveDate { get; private set; }

        /// <summary>
        /// The minimum payment amount allowed for this loan.
        /// </summary>
        public decimal MinimumPayment { get; set; }

        /// <summary>
        /// A human readable name for this loan
        /// </summary>
        public string LoanName { get; set; }

        public Loan()
        {
            Id = _idGenerator;
            _idGenerator++;
        }

        private Loan(int id, decimal principal, DateTime principalDt)
        {
            Id = id;
            Principal = principal;
            PrincipalEffectiveDate = principalDt;
        }

        /// <summary>
        /// Sets the <see cref="Principal"/> balance of the loan and its associated <see cref="PrincipalEffectiveDate"/>
        /// Deprecated. This is only here for backwards compatability at the moment. Specify the accrued interest going forward.
        /// </summary>
        /// <param name="principal">The loan's principal balance</param>
        /// <param name="asOf">The date os of which the <paramref name="principal"/> are being reported.</param>
        [Obsolete("Specify the accrued interest as well going foward.")]
        public void SetBalance(decimal principal, DateTime asOf)
        {
            Principal = principal;
            PrincipalEffectiveDate = asOf;
        }

        /// <summary>
        /// Sets the <see cref="Principal"/> balance & <see cref="AccruedInterest"/> of the loan and its associated <see cref="PrincipalEffectiveDate"/>
        /// </summary>
        /// <param name="principal">The loan's principal balance</param>
        /// <param name="interest">The accrued interest on the loan</param>
        /// <param name="asOf">The date as of which the <paramref name="principal"/> and accrued <paramref name="interest"/> are being set.</param>
        public void SetBalance(decimal principal, decimal interest, DateTime asOf)
        {
            Principal = principal;
            AccruedInterest = interest;
            PrincipalEffectiveDate = asOf;
        }

        /// <summary>
        /// Calcuates the amount of interest accruing daily on the loan based on the number of days in the year. 
        /// </summary>
        /// <returns></returns>
        public decimal CurrentDailyInterestRate()
        {
            return (Principal * InterestRate) / (decimal) PrincipalEffectiveDate.DaysInYear();
        }

        /// <summary>
        /// The interest accrued on the loan (calculated based on the number of days from the <see cref="PrincipalEffectiveDate"/>)
        /// </summary>
        /// <param name="asOf">The date to calculate accrued interest for</param>
        /// <returns>The interest accrued on the loan as of the provided date.</returns>
        /// <exception cref="InvalidOperationException">When the provided <paramref name="asOf"/> date is before the loan's <see cref="PrincipalEffectiveDate"/>.</exception>
        public decimal CalculateInterest(DateTime asOf)
        {
            if (asOf < PrincipalEffectiveDate) throw new InvalidOperationException();

            var total = 0.00m;

            for (var i = PrincipalEffectiveDate.Year; i <= asOf.Year; i++)
            {
                if (i == PrincipalEffectiveDate.Year)
                {
                    var dateDiff = (new DateTime(PrincipalEffectiveDate.Year, 12, 31) - PrincipalEffectiveDate).Days;
                    total += ((Principal * InterestRate) / (decimal)PrincipalEffectiveDate.DaysInYear()) * dateDiff;
                    continue;
                }
                if (i == asOf.Year)
                {
                    var dateDiff = (asOf - new DateTime(asOf.Year, 1, 1)).Days;
                    total += ((Principal * InterestRate) / (decimal)asOf.DaysInYear()) * dateDiff;
                    continue;
                }

                var daysInYear = new DateTime(i, 1, 1).DaysInYear();
                total += ((Principal * InterestRate) / (decimal)daysInYear) * (decimal)daysInYear;
            }

            return total;
        }

        /// <summary>
        /// The sum of the interest accrued up until <paramref name="asOf"/> and the <see cref="Principal"/> balance.
        /// </summary>
        /// <param name="asOf">The date to calculate accrued interest for</param>
        /// <returns>The total balance owed on the loan as of the provided date.</returns>
        /// <exception cref="InvalidOperationException">When the provided <paramref name="asOf"/> date is before the loan's <see cref="PrincipalEffectiveDate"/>.</exception>
        public decimal TotalOwed(DateTime asOf)
        {
            return CalculateInterest(asOf) + Principal;
        }

        /// <summary>
        /// The sum of <see cref="AccruedInterest"/> and <see cref="Principal"/>
        /// </summary>
        /// <returns>The total balance currently owed on the loan.</returns>
        /// <exception cref="InvalidOperationException">When the loan's <see cref="PrincipalEffectiveDate"/> is in the future.</exception>
        public decimal TotalOwed()
        {
            return AccruedInterest + Principal;
        }

        public Loan ProjectForward(DateTime to)
        {
            if (to < PrincipalEffectiveDate) throw new InvalidOperationException();

            var dateDiff = (to - PrincipalEffectiveDate).Days;

            if (dateDiff == 0) return this;


            var newPrincipal = 0.00m;
            if (to.Year == PrincipalEffectiveDate.Year)
            {
                newPrincipal = (CurrentDailyInterestRate() * (decimal)dateDiff) + Principal;
            }
            else
            {
                newPrincipal = ((PrincipalEffectiveDate.DaysInYear() - PrincipalEffectiveDate.DayOfYear) * CurrentDailyInterestRate()) + Principal;
                for (int i = PrincipalEffectiveDate.Year + 1; i < to.Year; i++)
                    newPrincipal *= (1 + InterestRate);

                newPrincipal += newPrincipal * (1 + InterestRate) / to.DaysInYear() * to.DayOfYear;
            }

            return new Loan(Id, newPrincipal, to) { InterestRate = InterestRate, LoanName = LoanName, MinimumPayment = MinimumPayment };
        }
    }
}

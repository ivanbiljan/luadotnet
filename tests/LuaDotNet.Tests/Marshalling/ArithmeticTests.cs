using System;
using Xunit;

namespace LuaDotNet.Tests.Marshalling
{
    public class ArithmeticTests
    {
        private struct Fraction
        {
            public Fraction(int numerator, int denominator)
            {
                if (denominator == 0)
                {
                    throw new ArgumentException("Denominator cannot be zero.", nameof(denominator));
                }

                Numerator = numerator;
                Denominator = denominator;
            }

            public int Numerator { get; }

            public int Denominator { get; }

            public static Fraction operator +(Fraction a) => a;
            public static Fraction operator -(Fraction a) => new Fraction(-a.Numerator, a.Denominator);

            public static Fraction operator +(Fraction a, Fraction b)
                => new Fraction(
                    a.Numerator * b.Denominator + b.Numerator * a.Denominator,
                    a.Denominator * b.Denominator);

            public static Fraction operator -(Fraction a, Fraction b)
                => a + -b;

            public static Fraction operator *(Fraction a, Fraction b)
                => new Fraction(a.Numerator * b.Numerator, a.Denominator * b.Denominator);

            public static Fraction operator /(Fraction a, Fraction b)
            {
                if (b.Numerator == 0)
                {
                    throw new DivideByZeroException();
                }

                return new Fraction(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
            }

            public override string ToString() => $"{Numerator} / {Denominator}";
        }

        [Fact]
        public void Add_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("a", new Fraction(5, 4));
                lua.SetGlobal("b", new Fraction(1, 2));

                var result = (Fraction)lua.DoString("return a + b")[0];

                Assert.Equal(14, result.Numerator);
                Assert.Equal(8, result.Denominator);
            }
        }

        [Fact]
        public void Divide_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("a", new Fraction(5, 4));
                lua.SetGlobal("b", new Fraction(1, 2));

                var result = (Fraction)lua.DoString("return a / b")[0];

                Assert.Equal(10, result.Numerator);
                Assert.Equal(4, result.Denominator);
            }
        }

        [Fact]
        public void Multiply_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("a", new Fraction(5, 4));
                lua.SetGlobal("b", new Fraction(1, 2));

                var result = (Fraction)lua.DoString("return a * b")[0];

                Assert.Equal(5, result.Numerator);
                Assert.Equal(8, result.Denominator);
            }
        }

        [Fact]
        public void Subtract_IsCorrect()
        {
            using (var lua = new LuaContext())
            {
                lua.SetGlobal("a", new Fraction(5, 4));
                lua.SetGlobal("b", new Fraction(1, 2));

                var result = (Fraction)lua.DoString("return a - b")[0];

                Assert.Equal(6, result.Numerator);
                Assert.Equal(8, result.Denominator);
            }
        }
    }
}
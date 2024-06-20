using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace gccphat_core
{

    /**
     * Performs an in-place complex FFT.
     *
     * Released under the MIT License
     *
     * Copyright (c) 2010 Gerald T. Beauregard
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to
     * deal in the Software without restriction, including without limitation the
     * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
     * sell copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     *
     * The above copyright notice and this permission notice shall be included in
     * all copies or substantial portions of the Software.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
     * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
     * IN THE SOFTWARE.
     */
    public class FFT2
    {
        private class FFTElement
        {
            public double Re;
            public double Im;
            public uint RevTgt;
        }

        private uint _logN;
        private uint _N;
        private FFTElement[] _elements;

        public FFT2()
        {
        }

        public void Init(uint logN)
        {
            _logN = logN;
            _N = 1u << (int)_logN;
            _elements = new FFTElement[_N];

            for (uint i = 0; i < _N; i++)
            {
                _elements[i] = new FFTElement();
            }

            for (uint i = 0; i < _N; i++)
            {
                _elements[i].RevTgt = BitReverse(i, _logN);
            }
        }

        /**
         * Performs in-place complex FFT.
         *
         * @param   xRe     Real part of input/output
         * @param   xIm     Imaginary part of input/output
         * @param   inverse If true, do an inverse FFT
         */
        public void Run(double[] xRe, double[] xIm, bool inverse = false)
        {
            double scale = inverse ? 1.0 / _N : 1.0;
            for (uint i = 0; i < _N; i++)
            {
                _elements[i].Re = scale * xRe[i];
                _elements[i].Im = scale * xIm[i];
            }

            uint numFlies = _N >> 1;
            uint span = _N >> 1;
            uint spacing = _N;
            uint wIndexStep = 1;

            for (uint stage = 0; stage < _logN; stage++)
            {
                double wAngleInc = wIndexStep * 2.0 * Math.PI / _N;
                if (!inverse)
                {
                    wAngleInc = -wAngleInc;
                }

                double wMulRe = Math.Cos(wAngleInc);
                double wMulIm = Math.Sin(wAngleInc);

                for (uint start = 0; start < _N; start += spacing)
                {
                    double wRe = 1.0;
                    double wIm = 0.0;

                    for (uint flyCount = 0; flyCount < numFlies; flyCount++)
                    {
                        FFTElement top = _elements[start + flyCount];
                        FFTElement bot = _elements[start + flyCount + span];

                        double topRe = top.Re;
                        double topIm = top.Im;
                        double botRe = bot.Re;
                        double botIm = bot.Im;

                        top.Re = topRe + botRe;
                        top.Im = topIm + botIm;

                        double diffRe = topRe - botRe;
                        double diffIm = topIm - botIm;

                        bot.Re = diffRe * wRe - diffIm * wIm;
                        bot.Im = diffRe * wIm + diffIm * wRe;

                        double tRe = wRe;
                        wRe = wRe * wMulRe - wIm * wMulIm;
                        wIm = tRe * wMulIm + wIm * wMulRe;
                    }
                }

                numFlies >>= 1;
                span >>= 1;
                spacing >>= 1;
                wIndexStep <<= 1;
            }

            for (uint i = 0; i < _N; i++)
            {
                uint target = _elements[i].RevTgt;
                xRe[target] = _elements[i].Re;
                xIm[target] = _elements[i].Im;
            }
        }

        /**
         * Do bit reversal of specified number of places of an int
         * For example, 1101 bit-reversed is 1011
         *
         * @param   x       Number to be bit-reverse.
         * @param   numBits Number of bits in the number.
         */
        private uint BitReverse(uint x, uint numBits)
        {
            uint y = 0;
            for (uint i = 0; i < numBits; i++)
            {
                y <<= 1;
                y |= x & 1;
                x >>= 1;
            }
            return y;
        }
    }

    public static class GccPhatCore
    {

        private static Dictionary<double, Complex> expLookupTable;
        private static double[] sortedPhases;
        private static int previousNfft = 0;
        private static int previousFs = 0;
        private static double[] vfc_pos;
        private static double[] vfc_neg;
        private static double[] vfc;
        private static Complex[] Pxy;
        private static Complex[] denom;
        private static double[] G;
        private static int[] axe_spl;
        private static double[] axe_ms;

        public static void Initialize(int nfft, int fs)
        {
            if (nfft == previousNfft && fs == previousFs)
            {
                // Already initialized for this nfft and fs
                return;
            }

            vfc_pos = Enumerable.Range(0, nfft / 2 + 1).Select(i => (double)i * fs / 2 / (nfft / 2)).ToArray();
            vfc_neg = vfc_pos.Skip(1).Take(vfc_pos.Length - 2).Select(i => -i).Reverse().ToArray();
            vfc = vfc_pos.Concat(vfc_neg).ToArray();

            previousNfft = nfft;
            previousFs = fs;

            Pxy = new Complex[nfft];
            denom = new Complex[nfft];
            G = new double[nfft];
            axe_spl = new int[nfft];
            axe_ms = new double[nfft];

            CreateExpLookupTable(360); // Initialize the lookup table with 360 points for example
        }

        private static void CreateExpLookupTable(int numPoints)
        {
            expLookupTable = new Dictionary<double, Complex>();
            double step = 2.0 * Math.PI / numPoints;
            sortedPhases = new double[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                double phase = i * step - Math.PI; // Phase from -π to π
                expLookupTable[phase] = Complex.Exp(Complex.ImaginaryOne * phase);
                sortedPhases[i] = phase;
            }
        }

        private static Complex GetExpFromLookup(double phase)
        {
            // Perform binary search to find the nearest phase
            int index = Array.BinarySearch(sortedPhases, phase);
            if (index < 0)
            {
                index = ~index;
                if (index == sortedPhases.Length || (index > 0 && Math.Abs(sortedPhases[index - 1] - phase) < Math.Abs(sortedPhases[index] - phase)))
                {
                    index--;
                }
            }
            return expLookupTable[sortedPhases[index]];
        }

        public static Complex[] ComputeSigoutComplex(Complex[] siginFFT, Complex[] absS)
        {
            int length = siginFFT.Length;
            Complex[] sigoutComplex = new Complex[length];

            Parallel.For(0, length, i =>
            {
                double phase = siginFFT[i].Phase;
                sigoutComplex[i] = absS[i] * GetExpFromLookup(phase);
            });

            return sigoutComplex;
        }
        public static Complex[] COLORE_FREQ(double[] sigin, int fs, int fmin, int fmax, int mode)
        {
            int nfft = sigin.Length;

            // Initialize frequency arrays if not already done
            Initialize(nfft, fs);

            Complex[] siginFFT = MYFFT(sigin);

            int[] indFreqInBand = new int[nfft];
            int[] indFreqOutBand = new int[nfft];
            int inBandCount = 0, outBandCount = 0;

            for (int i = 0; i < nfft; i++)
            {
                double vfcValue = vfc[i];
                if ((vfcValue >= fmin && vfcValue <= fmax) || (vfcValue <= -fmin && vfcValue >= -fmax))
                {
                    indFreqInBand[inBandCount++] = i;
                }
                else if ((vfcValue < fmin && vfcValue > -fmin) || (vfcValue < -fmax || vfcValue > fmax))
                {
                    indFreqOutBand[outBandCount++] = i;
                }
            }

            Complex[] absS = new Complex[siginFFT.Length];
            for (int i = 0; i < siginFFT.Length; i++)
            {
                absS[i] = new Complex(Math.Abs(siginFFT[i].Real), 0);
            }

            if (mode == 1)
            {
                for (int i = 0; i < outBandCount; i++)
                {
                    absS[indFreqOutBand[i]] = Complex.Zero;
                }
            }
            else
            {
                for (int i = 0; i < inBandCount; i++)
                {
                    absS[indFreqInBand[i]] = Complex.Zero;
                }
            }

            //Complex[] sigoutComplex = new Complex[siginFFT.Length];
            //for (int i = 0; i < siginFFT.Length; i++)
            //{
            //    sigoutComplex[i] = absS[i] * Complex.Exp(Complex.ImaginaryOne * siginFFT[i].Phase);
            //}

            //Complex[] sigoutComplex = new Complex[siginFFT.Length];

            //Parallel.For(0, siginFFT.Length, i =>
            //{
            //    sigoutComplex[i] = absS[i] * Complex.Exp(Complex.ImaginaryOne * siginFFT[i].Phase);
            //});

            // Compute sigoutComplex using the optimized function
            Complex[] sigoutComplex = ComputeSigoutComplex(siginFFT, absS);

            return sigoutComplex;
        }

        public static Complex[] MYFFT(double[] a)
        {
            try
            {
                FFT2 fft2 = new FFT2();
                fft2.Init((uint)Math.Log(a.Length, 2));
                double[] xRe = a.ToArray();
                double[] xIm = new double[a.Length];
                fft2.Run(xRe, xIm);
                return xRe.Select((re, i) => new Complex(re, xIm[i])).ToArray();

            }
            catch (Exception ex)
            {
                throw new Exception($"Exception in MYFFT: {ex.Message}", ex);
            }
        }

        public static double[] MYIFFT(Complex[] a)
        {
            try
            {      
                FFT2 fft2 = new FFT2();
                fft2.Init((uint)Math.Log(a.Length, 2));
                double[] xRe = a.Select(c => c.Real).ToArray();
                double[] xIm = a.Select(c => c.Imaginary).ToArray();
                fft2.Run(xRe, xIm, true);
                return xRe;                  
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception in MYIFFT: {ex.Message}", ex);
            }
        }

        public static double[] CircularShiftLeft(double[] array, int shiftLength)
        {
            int length = array.Length;
            double[] shiftedArray = new double[length];
            int shift = shiftLength % length;
            Array.Copy(array, shift, shiftedArray, 0, length - shift);
            Array.Copy(array, 0, shiftedArray, length - shift, shift);
            return shiftedArray;
        }

        public static double GCCPHAT(double[] s1, double[] s2, int fs, int norm, int fmin, int fmax)
        {
            Complex[] f_s1 = COLORE_FREQ(s1, fs, fmin, fmax, 1);
            Complex[] f_s2 = COLORE_FREQ(s2, fs, fmin, fmax, 1);

            int length = f_s1.Length;

            //Complex[] Pxy = new Complex[length];
            //Complex[] denom = new Complex[length];
            //double[] G;

            // Compute Pxy and denom arrays
            for (int i = 0; i < length; i++)
            {
                Pxy[i] = f_s1[i] * Complex.Conjugate(f_s2[i]);

                if (norm == 1)
                {
                    double absValue = Complex.Abs(Pxy[i]);
                    denom[i] = absValue < 1e-6 ? new Complex(1e-6, 0) : new Complex(absValue, 0);
                }
                else
                {
                    denom[i] = new Complex(1.0, 0);
                }
            }

            // Compute G array by dividing Pxy by denom
            Complex[] normalizedPxy = new Complex[length];
            for (int i = 0; i < length; i++)
            {
                normalizedPxy[i] = Pxy[i] / denom[i];
            }

            G = MYIFFT(normalizedPxy);

            // Find the maximum value in G for normalization
            //double maxG = G.Max();

            // Normalize G array
            //for (int i = 0; i < length; i++)
            //{
            //    G[i] /= maxG;
            //}

            // Circular shift G array
            //G = CircularShiftLeft(G, length / 2);

            //// Compute axe_spl and axe_ms arrays
            //int[] axe_spl = new int[length];
            //double[] axe_ms = new double[length];
            //for (int i = 0; i < length; i++)
            //{
            //    axe_spl[i] = i - length / 2;
            //    axe_ms[i] = (double)axe_spl[i] / fs * 1000;
            //}

            //return (G, axe_spl, axe_ms);

            int halfLength = length / 2;
            int maxIndex = 0;
            double maxG = double.MinValue;

            // Inline circular shift and compute time delay
            for (int i = 0; i < length; i++)
            {
                int shiftedIndex = (i + halfLength) % length;
                if (G[shiftedIndex] > maxG)
                {
                    maxG = G[shiftedIndex];
                    maxIndex = i;
                }
            }

            double timeDelay_ms = ((maxIndex - halfLength) / (double)fs) * 1000;
            return timeDelay_ms;
        }



        private static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }
    }
}

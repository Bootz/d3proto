﻿using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace d3emu
{
    public class SRP
    {
        private static readonly byte[] gBytes = new byte[] { 0x02 };

        private readonly BigInteger g = gBytes.ToPosBigInteger();

        private static readonly byte[] NBytes = new byte[]
            {
                0xAB, 0x24, 0x43, 0x63, 0xA9, 0xC2, 0xA6, 0xC3, 0x3B, 0x37, 0xE4, 0x61, 0x84, 0x25, 0x9F, 0x8B,
                0x3F, 0xCB, 0x8A, 0x85, 0x27, 0xFC, 0x3D, 0x87, 0xBE, 0xA0, 0x54, 0xD2, 0x38, 0x5D, 0x12, 0xB7,
                0x61, 0x44, 0x2E, 0x83, 0xFA, 0xC2, 0x21, 0xD9, 0x10, 0x9F, 0xC1, 0x9F, 0xEA, 0x50, 0xE3, 0x09,
                0xA6, 0xE5, 0x5E, 0x23, 0xA7, 0x77, 0xEB, 0x00, 0xC7, 0xBA, 0xBF, 0xF8, 0x55, 0x8A, 0x0E, 0x80,
                0x2B, 0x14, 0x1A, 0xA2, 0xD4, 0x43, 0xA9, 0xD4, 0xAF, 0xAD, 0xB5, 0xE1, 0xF5, 0xAC, 0xA6, 0x13,
                0x1C, 0x69, 0x78, 0x64, 0x0B, 0x7B, 0xAF, 0x9C, 0xC5, 0x50, 0x31, 0x8A, 0x23, 0x08, 0x01, 0xA1,
                0xF5, 0xFE, 0x31, 0x32, 0x7F, 0xE2, 0x05, 0x82, 0xD6, 0x0B, 0xED, 0x4D, 0x55, 0x32, 0x41, 0x94,
                0x29, 0x6F, 0x55, 0x7D, 0xE3, 0x0F, 0x77, 0x19, 0xE5, 0x6C, 0x30, 0xEB, 0xDE, 0xF6, 0xA7, 0x86
            };

        private readonly BigInteger N = NBytes.ToPosBigInteger();

        private static SHA256Managed HASH = new SHA256Managed();

        private readonly BigInteger s;
        private readonly BigInteger I;
        private readonly BigInteger v;
        private readonly BigInteger b;
        private readonly BigInteger B;

        private static string Test_I = HASH.ComputeHash(Encoding.ASCII.GetBytes("TEST@TEST:PASS")).ToHexString();

        // I is SHA256(account+":"+password)
        public SRP(string _I)
        {
            var sBytes = GetRandomBytes(32);
            s = sBytes.ToPosBigInteger();

            var IBytes = _I.ToByteArray();
            I = IBytes.ToPosBigInteger();

            var xBytes = HASH.ComputeHash(new byte[0]
                .Concat(sBytes)
                .Concat(IBytes)
                .ToArray());

            var x = xBytes.ToPosBigInteger();

            v = BigInteger.ModPow(g, x, N);

            b = GetRandomBytes(128).ToPosBigInteger();

            var gMod = BigInteger.ModPow(g, b, N);

            var kBytes = HASH.ComputeHash(new byte[0]
                .Concat(NBytes)
                .Concat(gBytes)
                .ToArray());

            var k = kBytes.ToPosBigInteger();

            B = BigInteger.Remainder((v * k) + gMod, N); // Remainder = Mod?
        }

        public bool Verify(byte[] ABytes, byte[] M1Bytes)
        {
            var A = ABytes.ToPosBigInteger();

            var uBytes = HASH.ComputeHash(new byte[0]
                .Concat(ABytes)
                .Concat(B.ToByteArray())
                .ToArray());

            var u = uBytes.ToPosBigInteger();

            var S = BigInteger.ModPow(A * BigInteger.ModPow(v, u, N), b, N);

            var KBytes = Calc_K(S.ToByteArray());

            var t3Bytes = Hash_g_and_N_and_xor_them();

            var M = HASH.ComputeHash(new byte[0]
                .Concat(t3Bytes)
                .Concat(I.ToByteArray()) // ?
                .Concat(s.ToByteArray())
                .Concat(ABytes)
                .Concat(B.ToByteArray())
                .Concat(KBytes)
                .ToArray());

            if (!M.CompareTo(M1Bytes))
                return false;

            // M2 is sent to client
            var M2 = HASH.ComputeHash(new byte[0]
                .Concat(ABytes)
                .Concat(M)
                .Concat(KBytes)
                .ToArray());

            return true;
        }

        //  Interleave SHA256 Key
        private byte[] Calc_K(byte[] S)
        {
            var K = new byte[64];

            var half_S = new byte[64];

            for (int i = 0; i < 64; ++i)
            {
                half_S[i] = S[i * 2];
            }

            var p1 = HASH.ComputeHash(half_S);

            for (int i = 0; i < 32; ++i)
            {
                K[i * 2] = p1[i];
            }

            for (int i = 0; i < 64; ++i)
            {
                half_S[i] = S[i * 2 + 1];
            }

            var p2 = HASH.ComputeHash(half_S);

            for (int i = 0; i < 32; ++i)
            {
                K[i * 2 + 1] = p2[i];
            }

            return K;
        }

        private byte[] Hash_g_and_N_and_xor_them()
        {
            var hash_N = HASH.ComputeHash(NBytes);
            var hash_g = HASH.ComputeHash(gBytes);

            for (var i = 0; i < 32; ++i)
            {
                hash_N[i] ^= hash_g[i];
            }

            return hash_N;
        }

        private static byte[] GetRandomBytes(int count)
        {
            Random rnd = new Random();
            var result = new byte[count];
            rnd.NextBytes(result);
            return result;
        }
    }
}
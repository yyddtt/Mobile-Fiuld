#version 310 es
layout(local_size_x = 128, local_size_y = 1, local_size_z = 1) in;
precision highp float;
layout(std430, binding = 1) buffer gtmp_i32 { int _gtmp_i32_[];}; 
layout(std430, binding = 1) buffer gtmp_f32 { float _gtmp_f32_[];}; 
layout(std430, binding = 2) buffer args_i32 { int _args_i32_[];}; 
layout(std430, binding = 2) buffer args_f32 { float _args_f32_[];}; 
layout(std430, binding = 9) buffer arr1_i32 { int _arr1_i32_[];}; 
layout(std430, binding = 9) buffer arr1_f32 { float _arr1_f32_[];}; 
layout(std430, binding = 8) buffer arr3_i32 { int _arr3_i32_[];}; 
layout(std430, binding = 8) buffer arr3_f32 { float _arr3_f32_[];}; 
layout(std430, binding = 7) buffer arr2_i32 { int _arr2_i32_[];}; 
layout(std430, binding = 7) buffer arr2_f32 { float _arr2_f32_[];}; 
layout(std430, binding = 6) buffer arr0_i32 { int _arr0_i32_[];}; 
layout(std430, binding = 6) buffer arr0_f32 { float _arr0_f32_[];}; 
layout(std430, binding = 5) buffer arr5_i32 { int _arr5_i32_[];}; 
layout(std430, binding = 5) buffer arr5_f32 { float _arr5_f32_[];}; 
layout(std430, binding = 4) buffer arr4_i32 { int _arr4_i32_[];}; 
layout(std430, binding = 4) buffer arr4_f32 { float _arr4_f32_[];}; 
float atomicAdd_gtmp_f32(int addr, float rhs) { int old, new, ret; do { old = _gtmp_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_gtmp_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_gtmp_f32(int addr, float rhs) { int old, new, ret; do { old = _gtmp_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_gtmp_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_gtmp_f32(int addr, float rhs) { int old, new, ret; do { old = _gtmp_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_gtmp_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_gtmp_f32(int addr, float rhs) { int old, new, ret; do { old = _gtmp_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_gtmp_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr0_f32(int addr, float rhs) { int old, new, ret; do { old = _arr0_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr0_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr0_f32(int addr, float rhs) { int old, new, ret; do { old = _arr0_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr0_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr0_f32(int addr, float rhs) { int old, new, ret; do { old = _arr0_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr0_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr0_f32(int addr, float rhs) { int old, new, ret; do { old = _arr0_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr0_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr1_f32(int addr, float rhs) { int old, new, ret; do { old = _arr1_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr1_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr1_f32(int addr, float rhs) { int old, new, ret; do { old = _arr1_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr1_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr1_f32(int addr, float rhs) { int old, new, ret; do { old = _arr1_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr1_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr1_f32(int addr, float rhs) { int old, new, ret; do { old = _arr1_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr1_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr2_f32(int addr, float rhs) { int old, new, ret; do { old = _arr2_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr2_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr2_f32(int addr, float rhs) { int old, new, ret; do { old = _arr2_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr2_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr2_f32(int addr, float rhs) { int old, new, ret; do { old = _arr2_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr2_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr2_f32(int addr, float rhs) { int old, new, ret; do { old = _arr2_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr2_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr3_f32(int addr, float rhs) { int old, new, ret; do { old = _arr3_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr3_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr3_f32(int addr, float rhs) { int old, new, ret; do { old = _arr3_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr3_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr3_f32(int addr, float rhs) { int old, new, ret; do { old = _arr3_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr3_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr3_f32(int addr, float rhs) { int old, new, ret; do { old = _arr3_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr3_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr4_f32(int addr, float rhs) { int old, new, ret; do { old = _arr4_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr4_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr4_f32(int addr, float rhs) { int old, new, ret; do { old = _arr4_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr4_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr4_f32(int addr, float rhs) { int old, new, ret; do { old = _arr4_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr4_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr4_f32(int addr, float rhs) { int old, new, ret; do { old = _arr4_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr4_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr5_f32(int addr, float rhs) { int old, new, ret; do { old = _arr5_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr5_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr5_f32(int addr, float rhs) { int old, new, ret; do { old = _arr5_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr5_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr5_f32(int addr, float rhs) { int old, new, ret; do { old = _arr5_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr5_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr5_f32(int addr, float rhs) { int old, new, ret; do { old = _arr5_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr5_i32_[addr], old, new)); return intBitsToFloat(old); }
const float inf = 1.0f / 0.0f;
const float nan = 0.0f / 0.0f;
void substep_c54_07()
{ // range for
  // range known at runtime
  int _beg = 0, _end = _gtmp_i32_[28 >> 2];
  int _sid0 = int(gl_GlobalInvocationID.x);
  for (int _sid = _sid0; _sid < (_end - _beg); _sid += int(gl_WorkGroupSize.x * gl_NumWorkGroups.x)) {
    int _itv = _beg + _sid;
      int G9 = _itv;
      int Ga = 28;
      int Gb = _gtmp_i32_[Ga >> 2];
      int Gc = G9 - Gb * int(G9 / Gb);
      int Ge = int(0);
      int _li_Gf = 0;
      { // linear seek
        int _s0_Gf = _args_i32_[16 + 0 * 8 + 0];
        int _s1_Gf = _args_i32_[16 + 0 * 8 + 1];
        _li_Gf *= _s0_Gf;
        _li_Gf += Gc;
        _li_Gf *= _s1_Gf;
        _li_Gf += Ge;
      }
      int Gf = _li_Gf << 2;
      float Gg = _arr0_f32_[Gf >> 2];
      float Gh = float(128.0);
      float Gi = Gg * Gh;
      int Gj = int(1);
      int _li_Gk = 0;
      { // linear seek
        int _s0_Gk = _args_i32_[16 + 0 * 8 + 0];
        int _s1_Gk = _args_i32_[16 + 0 * 8 + 1];
        _li_Gk *= _s0_Gk;
        _li_Gk += Gc;
        _li_Gk *= _s1_Gk;
        _li_Gk += Gj;
      }
      int Gk = _li_Gk << 2;
      float Gl = _arr0_f32_[Gk >> 2];
      float Gm = Gl * Gh;
      float Gn = float(0.5);
      float Go = Gi - Gn;
      float Gp = Gm - Gn;
      int Gq = int(Go);
      int Gr = int(Gp);
      float Gs = float(Gq);
      float Gt = float(Gr);
      float Gu = Gi - Gs;
      float Gv = Gm - Gt;
      float Gw = float(1.5);
      float Gx = Gw - Gu;
      float Gy = Gw - Gv;
      float Gz = Gx * Gx;
      float GA = Gy * Gy;
      float GB = Gz * Gn;
      float GC = GA * Gn;
      float GD = float(1.0);
      float GE = Gu - GD;
      float GF = Gv - GD;
      float GG = GE * GE;
      float GH = GF * GF;
      float GI = float(0.75);
      float GJ = GI - GG;
      float GK = GI - GH;
      float GL = Gu - Gn;
      float GM = Gv - Gn;
      float GN = GL * GL;
      float GO = GM * GM;
      float GP = GN * Gn;
      float GQ = GO * Gn;
      float GX = float(0.0);
      float GY = GX - Gu;
      float GZ = GX - Gv;
      int _li_H1 = 0;
      { // linear seek
        int _s0_H1 = _args_i32_[16 + 4 * 8 + 0];
        int _s1_H1 = _args_i32_[16 + 4 * 8 + 1];
        int _s2_H1 = _args_i32_[16 + 4 * 8 + 2];
        _li_H1 *= _s0_H1;
        _li_H1 += Gq;
        _li_H1 *= _s1_H1;
        _li_H1 += Gr;
        _li_H1 *= _s2_H1;
        _li_H1 += Ge;
      }
      int H1 = _li_H1 << 2;
      float H2 = _arr4_f32_[H1 >> 2];
      int _li_H3 = 0;
      { // linear seek
        int _s0_H3 = _args_i32_[16 + 4 * 8 + 0];
        int _s1_H3 = _args_i32_[16 + 4 * 8 + 1];
        int _s2_H3 = _args_i32_[16 + 4 * 8 + 2];
        _li_H3 *= _s0_H3;
        _li_H3 += Gq;
        _li_H3 *= _s1_H3;
        _li_H3 += Gr;
        _li_H3 *= _s2_H3;
        _li_H3 += Gj;
      }
      int H3 = _li_H3 << 2;
      float H4 = _arr4_f32_[H3 >> 2];
      float H5 = GB * GC;
      float H6 = H5 * H2;
      float H7 = H5 * H4;
      float He = H2 * GY;
      float Hf = H2 * GZ;
      float Hg = H4 * GY;
      float Hh = H4 * GZ;
      float Hi = float(4.0);
      float Hj = H5 * Hi;
      float Hk = Hj * He;
      float Hl = Hj * Hf;
      float Hm = Hj * Hg;
      float Hn = Hj * Hh;
      float Ho = Hk * Gh;
      float Hp = Hl * Gh;
      float Hq = Hm * Gh;
      float Hr = Hn * Gh;
      float HE = GD - Gv;
      int HF = Gr + Gj;
      int _li_HG = 0;
      { // linear seek
        int _s0_HG = _args_i32_[16 + 4 * 8 + 0];
        int _s1_HG = _args_i32_[16 + 4 * 8 + 1];
        int _s2_HG = _args_i32_[16 + 4 * 8 + 2];
        _li_HG *= _s0_HG;
        _li_HG += Gq;
        _li_HG *= _s1_HG;
        _li_HG += HF;
        _li_HG *= _s2_HG;
        _li_HG += Ge;
      }
      int HG = _li_HG << 2;
      float HH = _arr4_f32_[HG >> 2];
      int _li_HI = 0;
      { // linear seek
        int _s0_HI = _args_i32_[16 + 4 * 8 + 0];
        int _s1_HI = _args_i32_[16 + 4 * 8 + 1];
        int _s2_HI = _args_i32_[16 + 4 * 8 + 2];
        _li_HI *= _s0_HI;
        _li_HI += Gq;
        _li_HI *= _s1_HI;
        _li_HI += HF;
        _li_HI *= _s2_HI;
        _li_HI += Gj;
      }
      int HI = _li_HI << 2;
      float HJ = _arr4_f32_[HI >> 2];
      float HK = GB * GK;
      float HL = HK * HH;
      float HM = HK * HJ;
      float HO = H6 + HL;
      float HR = H7 + HM;
      float HT = HH * GY;
      float HU = HH * HE;
      float HV = HJ * GY;
      float HW = HJ * HE;
      float HX = HK * Hi;
      float HY = HX * HT;
      float HZ = HX * HU;
      float I0 = HX * HV;
      float I1 = HX * HW;
      float I2 = HY * Gh;
      float I3 = HZ * Gh;
      float I4 = I0 * Gh;
      float I5 = I1 * Gh;
      float I7 = Ho + I2;
      float Ia = Hp + I3;
      float Id = Hq + I4;
      float Ig = Hr + I5;
      float Ii = float(2.0);
      float Ij = Ii - Gv;
      int Ik = int(2);
      int Il = Gr + Ik;
      int _li_Im = 0;
      { // linear seek
        int _s0_Im = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Im = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Im = _args_i32_[16 + 4 * 8 + 2];
        _li_Im *= _s0_Im;
        _li_Im += Gq;
        _li_Im *= _s1_Im;
        _li_Im += Il;
        _li_Im *= _s2_Im;
        _li_Im += Ge;
      }
      int Im = _li_Im << 2;
      float In = _arr4_f32_[Im >> 2];
      int _li_Io = 0;
      { // linear seek
        int _s0_Io = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Io = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Io = _args_i32_[16 + 4 * 8 + 2];
        _li_Io *= _s0_Io;
        _li_Io += Gq;
        _li_Io *= _s1_Io;
        _li_Io += Il;
        _li_Io *= _s2_Io;
        _li_Io += Gj;
      }
      int Io = _li_Io << 2;
      float Ip = _arr4_f32_[Io >> 2];
      float Iq = GB * GQ;
      float Ir = Iq * In;
      float Is = Iq * Ip;
      float Iu = HO + Ir;
      float Ix = HR + Is;
      float Iz = In * GY;
      float IA = In * Ij;
      float IB = Ip * GY;
      float IC = Ip * Ij;
      float ID = Iq * Hi;
      float IE = ID * Iz;
      float IF = ID * IA;
      float IG = ID * IB;
      float IH = ID * IC;
      float II = IE * Gh;
      float IJ = IF * Gh;
      float IK = IG * Gh;
      float IL = IH * Gh;
      float IN = I7 + II;
      float IQ = Ia + IJ;
      float IT = Id + IK;
      float IW = Ig + IL;
      float IY = GD - Gu;
      int IZ = Gq + Gj;
      int _li_J0 = 0;
      { // linear seek
        int _s0_J0 = _args_i32_[16 + 4 * 8 + 0];
        int _s1_J0 = _args_i32_[16 + 4 * 8 + 1];
        int _s2_J0 = _args_i32_[16 + 4 * 8 + 2];
        _li_J0 *= _s0_J0;
        _li_J0 += IZ;
        _li_J0 *= _s1_J0;
        _li_J0 += Gr;
        _li_J0 *= _s2_J0;
        _li_J0 += Ge;
      }
      int J0 = _li_J0 << 2;
      float J1 = _arr4_f32_[J0 >> 2];
      int _li_J2 = 0;
      { // linear seek
        int _s0_J2 = _args_i32_[16 + 4 * 8 + 0];
        int _s1_J2 = _args_i32_[16 + 4 * 8 + 1];
        int _s2_J2 = _args_i32_[16 + 4 * 8 + 2];
        _li_J2 *= _s0_J2;
        _li_J2 += IZ;
        _li_J2 *= _s1_J2;
        _li_J2 += Gr;
        _li_J2 *= _s2_J2;
        _li_J2 += Gj;
      }
      int J2 = _li_J2 << 2;
      float J3 = _arr4_f32_[J2 >> 2];
      float J4 = GJ * GC;
      float J5 = J4 * J1;
      float J6 = J4 * J3;
      float J8 = Iu + J5;
      float Jb = Ix + J6;
      float Jd = J1 * IY;
      float Je = J1 * GZ;
      float Jf = J3 * IY;
      float Jg = J3 * GZ;
      float Jh = J4 * Hi;
      float Ji = Jh * Jd;
      float Jj = Jh * Je;
      float Jk = Jh * Jf;
      float Jl = Jh * Jg;
      float Jm = Ji * Gh;
      float Jn = Jj * Gh;
      float Jo = Jk * Gh;
      float Jp = Jl * Gh;
      float Jr = IN + Jm;
      float Ju = IQ + Jn;
      float Jx = IT + Jo;
      float JA = IW + Jp;
      int _li_JC = 0;
      { // linear seek
        int _s0_JC = _args_i32_[16 + 4 * 8 + 0];
        int _s1_JC = _args_i32_[16 + 4 * 8 + 1];
        int _s2_JC = _args_i32_[16 + 4 * 8 + 2];
        _li_JC *= _s0_JC;
        _li_JC += IZ;
        _li_JC *= _s1_JC;
        _li_JC += HF;
        _li_JC *= _s2_JC;
        _li_JC += Ge;
      }
      int JC = _li_JC << 2;
      float JD = _arr4_f32_[JC >> 2];
      int _li_JE = 0;
      { // linear seek
        int _s0_JE = _args_i32_[16 + 4 * 8 + 0];
        int _s1_JE = _args_i32_[16 + 4 * 8 + 1];
        int _s2_JE = _args_i32_[16 + 4 * 8 + 2];
        _li_JE *= _s0_JE;
        _li_JE += IZ;
        _li_JE *= _s1_JE;
        _li_JE += HF;
        _li_JE *= _s2_JE;
        _li_JE += Gj;
      }
      int JE = _li_JE << 2;
      float JF = _arr4_f32_[JE >> 2];
      float JG = GJ * GK;
      float JH = JG * JD;
      float JI = JG * JF;
      float JK = J8 + JH;
      float JN = Jb + JI;
      float JP = JD * IY;
      float JQ = JD * HE;
      float JR = JF * IY;
      float JS = JF * HE;
      float JT = JG * Hi;
      float JU = JT * JP;
      float JV = JT * JQ;
      float JW = JT * JR;
      float JX = JT * JS;
      float JY = JU * Gh;
      float JZ = JV * Gh;
      float K0 = JW * Gh;
      float K1 = JX * Gh;
      float K3 = Jr + JY;
      float K6 = Ju + JZ;
      float K9 = Jx + K0;
      float Kc = JA + K1;
      int _li_Ke = 0;
      { // linear seek
        int _s0_Ke = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Ke = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Ke = _args_i32_[16 + 4 * 8 + 2];
        _li_Ke *= _s0_Ke;
        _li_Ke += IZ;
        _li_Ke *= _s1_Ke;
        _li_Ke += Il;
        _li_Ke *= _s2_Ke;
        _li_Ke += Ge;
      }
      int Ke = _li_Ke << 2;
      float Kf = _arr4_f32_[Ke >> 2];
      int _li_Kg = 0;
      { // linear seek
        int _s0_Kg = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Kg = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Kg = _args_i32_[16 + 4 * 8 + 2];
        _li_Kg *= _s0_Kg;
        _li_Kg += IZ;
        _li_Kg *= _s1_Kg;
        _li_Kg += Il;
        _li_Kg *= _s2_Kg;
        _li_Kg += Gj;
      }
      int Kg = _li_Kg << 2;
      float Kh = _arr4_f32_[Kg >> 2];
      float Ki = GJ * GQ;
      float Kj = Ki * Kf;
      float Kk = Ki * Kh;
      float Km = JK + Kj;
      float Kp = JN + Kk;
      float Kr = Kf * IY;
      float Ks = Kf * Ij;
      float Kt = Kh * IY;
      float Ku = Kh * Ij;
      float Kv = Ki * Hi;
      float Kw = Kv * Kr;
      float Kx = Kv * Ks;
      float Ky = Kv * Kt;
      float Kz = Kv * Ku;
      float KA = Kw * Gh;
      float KB = Kx * Gh;
      float KC = Ky * Gh;
      float KD = Kz * Gh;
      float KF = K3 + KA;
      float KI = K6 + KB;
      float KL = K9 + KC;
      float KO = Kc + KD;
      float KQ = Ii - Gu;
      int KR = Gq + Ik;
      int _li_KS = 0;
      { // linear seek
        int _s0_KS = _args_i32_[16 + 4 * 8 + 0];
        int _s1_KS = _args_i32_[16 + 4 * 8 + 1];
        int _s2_KS = _args_i32_[16 + 4 * 8 + 2];
        _li_KS *= _s0_KS;
        _li_KS += KR;
        _li_KS *= _s1_KS;
        _li_KS += Gr;
        _li_KS *= _s2_KS;
        _li_KS += Ge;
      }
      int KS = _li_KS << 2;
      float KT = _arr4_f32_[KS >> 2];
      int _li_KU = 0;
      { // linear seek
        int _s0_KU = _args_i32_[16 + 4 * 8 + 0];
        int _s1_KU = _args_i32_[16 + 4 * 8 + 1];
        int _s2_KU = _args_i32_[16 + 4 * 8 + 2];
        _li_KU *= _s0_KU;
        _li_KU += KR;
        _li_KU *= _s1_KU;
        _li_KU += Gr;
        _li_KU *= _s2_KU;
        _li_KU += Gj;
      }
      int KU = _li_KU << 2;
      float KV = _arr4_f32_[KU >> 2];
      float KW = GP * GC;
      float KX = KW * KT;
      float KY = KW * KV;
      float L0 = Km + KX;
      float L3 = Kp + KY;
      float L5 = KT * KQ;
      float L6 = KT * GZ;
      float L7 = KV * KQ;
      float L8 = KV * GZ;
      float L9 = KW * Hi;
      float La = L9 * L5;
      float Lb = L9 * L6;
      float Lc = L9 * L7;
      float Ld = L9 * L8;
      float Le = La * Gh;
      float Lf = Lb * Gh;
      float Lg = Lc * Gh;
      float Lh = Ld * Gh;
      float Lj = KF + Le;
      float Lm = KI + Lf;
      float Lp = KL + Lg;
      float Ls = KO + Lh;
      int _li_Lu = 0;
      { // linear seek
        int _s0_Lu = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Lu = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Lu = _args_i32_[16 + 4 * 8 + 2];
        _li_Lu *= _s0_Lu;
        _li_Lu += KR;
        _li_Lu *= _s1_Lu;
        _li_Lu += HF;
        _li_Lu *= _s2_Lu;
        _li_Lu += Ge;
      }
      int Lu = _li_Lu << 2;
      float Lv = _arr4_f32_[Lu >> 2];
      int _li_Lw = 0;
      { // linear seek
        int _s0_Lw = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Lw = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Lw = _args_i32_[16 + 4 * 8 + 2];
        _li_Lw *= _s0_Lw;
        _li_Lw += KR;
        _li_Lw *= _s1_Lw;
        _li_Lw += HF;
        _li_Lw *= _s2_Lw;
        _li_Lw += Gj;
      }
      int Lw = _li_Lw << 2;
      float Lx = _arr4_f32_[Lw >> 2];
      float Ly = GP * GK;
      float Lz = Ly * Lv;
      float LA = Ly * Lx;
      float LC = L0 + Lz;
      float LF = L3 + LA;
      float LH = Lv * KQ;
      float LI = Lv * HE;
      float LJ = Lx * KQ;
      float LK = Lx * HE;
      float LL = Ly * Hi;
      float LM = LL * LH;
      float LN = LL * LI;
      float LO = LL * LJ;
      float LP = LL * LK;
      float LQ = LM * Gh;
      float LR = LN * Gh;
      float LS = LO * Gh;
      float LT = LP * Gh;
      float LV = Lj + LQ;
      float LY = Lm + LR;
      float M1 = Lp + LS;
      float M4 = Ls + LT;
      int _li_M6 = 0;
      { // linear seek
        int _s0_M6 = _args_i32_[16 + 4 * 8 + 0];
        int _s1_M6 = _args_i32_[16 + 4 * 8 + 1];
        int _s2_M6 = _args_i32_[16 + 4 * 8 + 2];
        _li_M6 *= _s0_M6;
        _li_M6 += KR;
        _li_M6 *= _s1_M6;
        _li_M6 += Il;
        _li_M6 *= _s2_M6;
        _li_M6 += Ge;
      }
      int M6 = _li_M6 << 2;
      float M7 = _arr4_f32_[M6 >> 2];
      int _li_M8 = 0;
      { // linear seek
        int _s0_M8 = _args_i32_[16 + 4 * 8 + 0];
        int _s1_M8 = _args_i32_[16 + 4 * 8 + 1];
        int _s2_M8 = _args_i32_[16 + 4 * 8 + 2];
        _li_M8 *= _s0_M8;
        _li_M8 += KR;
        _li_M8 *= _s1_M8;
        _li_M8 += Il;
        _li_M8 *= _s2_M8;
        _li_M8 += Gj;
      }
      int M8 = _li_M8 << 2;
      float M9 = _arr4_f32_[M8 >> 2];
      float Ma = GP * GQ;
      float Mb = Ma * M7;
      float Mc = Ma * M9;
      float Me = LC + Mb;
      float Mh = LF + Mc;
      float Mj = M7 * KQ;
      float Mk = M7 * Ij;
      float Ml = M9 * KQ;
      float Mm = M9 * Ij;
      float Mn = Ma * Hi;
      float Mo = Mn * Mj;
      float Mp = Mn * Mk;
      float Mq = Mn * Ml;
      float Mr = Mn * Mm;
      float Ms = Mo * Gh;
      float Mt = Mp * Gh;
      float Mu = Mq * Gh;
      float Mv = Mr * Gh;
      float Mx = LV + Ms;
      float MA = LY + Mt;
      float MD = M1 + Mu;
      float MG = M4 + Mv;
      int _li_MK = 0;
      { // linear seek
        int _s0_MK = _args_i32_[16 + 1 * 8 + 0];
        int _s1_MK = _args_i32_[16 + 1 * 8 + 1];
        _li_MK *= _s0_MK;
        _li_MK += Gc;
        _li_MK *= _s1_MK;
        _li_MK += Ge;
      }
      int MK = _li_MK << 2;
      _arr1_f32_[MK >> 2] = Me;
      int _li_MN = 0;
      { // linear seek
        int _s0_MN = _args_i32_[16 + 1 * 8 + 0];
        int _s1_MN = _args_i32_[16 + 1 * 8 + 1];
        _li_MN *= _s0_MN;
        _li_MN += Gc;
        _li_MN *= _s1_MN;
        _li_MN += Gj;
      }
      int MN = _li_MN << 2;
      _arr1_f32_[MN >> 2] = Mh;
      float MP = _arr1_f32_[MK >> 2];
      float MQ = float(0.0002);
      float MR = MP * MQ;
      float MS = Mh * MQ;
      float MT;
      { // Begin Atomic Op
      MT = atomicAdd_arr0_f32(Gf >> 2, MR);
      } // End Atomic Op
      float MU;
      { // Begin Atomic Op
      MU = atomicAdd_arr0_f32(Gk >> 2, MS);
      } // End Atomic Op
      int _li_MW = 0;
      { // linear seek
        int _s0_MW = _args_i32_[16 + 2 * 8 + 0];
        _li_MW *= _s0_MW;
        _li_MW += Gc;
      }
      int MW = _li_MW << 2;
      float MX = _arr2_f32_[MW >> 2];
      float N0 = Mx + MG;
      float N1 = N0 * MQ;
      float N2 = N1 + GD;
      float N3 = MX * N2;
      _arr2_f32_[MW >> 2] = N3;
      int _li_N6 = 0;
      { // linear seek
        int _s0_N6 = _args_i32_[16 + 3 * 8 + 0];
        int _s1_N6 = _args_i32_[16 + 3 * 8 + 1];
        int _s2_N6 = _args_i32_[16 + 3 * 8 + 2];
        _li_N6 *= _s0_N6;
        _li_N6 += Gc;
        _li_N6 *= _s1_N6;
        _li_N6 += Ge;
        _li_N6 *= _s2_N6;
        _li_N6 += Ge;
      }
      int N6 = _li_N6 << 2;
      _arr3_f32_[N6 >> 2] = Mx;
      int _li_N9 = 0;
      { // linear seek
        int _s0_N9 = _args_i32_[16 + 3 * 8 + 0];
        int _s1_N9 = _args_i32_[16 + 3 * 8 + 1];
        int _s2_N9 = _args_i32_[16 + 3 * 8 + 2];
        _li_N9 *= _s0_N9;
        _li_N9 += Gc;
        _li_N9 *= _s1_N9;
        _li_N9 += Ge;
        _li_N9 *= _s2_N9;
        _li_N9 += Gj;
      }
      int N9 = _li_N9 << 2;
      _arr3_f32_[N9 >> 2] = MA;
      int _li_Nc = 0;
      { // linear seek
        int _s0_Nc = _args_i32_[16 + 3 * 8 + 0];
        int _s1_Nc = _args_i32_[16 + 3 * 8 + 1];
        int _s2_Nc = _args_i32_[16 + 3 * 8 + 2];
        _li_Nc *= _s0_Nc;
        _li_Nc += Gc;
        _li_Nc *= _s1_Nc;
        _li_Nc += Gj;
        _li_Nc *= _s2_Nc;
        _li_Nc += Ge;
      }
      int Nc = _li_Nc << 2;
      _arr3_f32_[Nc >> 2] = MD;
      int _li_Ne = 0;
      { // linear seek
        int _s0_Ne = _args_i32_[16 + 3 * 8 + 0];
        int _s1_Ne = _args_i32_[16 + 3 * 8 + 1];
        int _s2_Ne = _args_i32_[16 + 3 * 8 + 2];
        _li_Ne *= _s0_Ne;
        _li_Ne += Gc;
        _li_Ne *= _s1_Ne;
        _li_Ne += Gj;
        _li_Ne *= _s2_Ne;
        _li_Ne += Gj;
      }
      int Ne = _li_Ne << 2;
      _arr3_f32_[Ne >> 2] = MG;
  }
}

void main()
{
  substep_c54_07();
}

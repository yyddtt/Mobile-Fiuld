#pragma kernel main
static const uint3 gl_WorkGroupSize = uint3(8u, 1u, 1u);

RWByteAddressBuffer _68 : register(u0);
cbuffer SPIRV_Cross_NumWorkgroups
{
    uint3 SPIRV_Cross_NumWorkgroups_1_count : packoffset(c0);
};


static uint3 gl_GlobalInvocationID;
struct SPIRV_Cross_Input
{
    uint3 gl_GlobalInvocationID : SV_DispatchThreadID;
};

void test1_c54_00()
{
    int _sid0 = int(gl_GlobalInvocationID.x);
    for (int _sid = _sid0; _sid < 8; _sid += int(8u * SPIRV_Cross_NumWorkgroups_1_count.x))
    {
        int _itv = 0 + _sid;
        int C = _itv;
        int J = 1;
        int Bj = 0;
        int BA = 0;
        int Bl = Bj + (96 * BA);
        int Bm = Bl + 0;
        int BH = 7;
        int BG = C & BH;
        int Bp = Bm + (4 * BG);
        int Bq = Bp + 0;
        _68.Store((Bq >> 2) * 4 + 0, uint(J));
    }
}

void comp_main()
{
    test1_c54_00();
}

[numthreads(8, 1, 1)]
void main(SPIRV_Cross_Input stage_input)
{
    gl_GlobalInvocationID = stage_input.gl_GlobalInvocationID;
    comp_main();
}

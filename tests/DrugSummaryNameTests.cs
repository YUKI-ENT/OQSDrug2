using System;
using OQSDrug;

internal static class DrugSummaryNameTests
{
    private static int assertions;

    private static void Check(string original, string expected, bool halfWidth = true)
    {
        string actual = DrugSummaryName.Format(original, halfWidth);
        assertions++;
        if (actual != expected)
            throw new Exception(original + ": expected=" + expected + ", actual=" + actual);
        if (DrugSummaryName.Format(actual, halfWidth) != actual)
            throw new Exception("Formatting must be idempotent: " + original);
    }

    private static int Main()
    {
        try { return Run(); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message);
            return 1;
        }
    }

    private static int Run()
    {
        Check("ビラノアOD錠20mg", "ﾋﾞﾗﾉｱ");
        Check("ビラノアＯＤ錠２０ｍｇ", "ビラノア", false);
        Check("ﾋﾞﾗﾉｱOD錠20mg", "ﾋﾞﾗﾉｱ");
        Check("スルピリド錠５０ｍｇ「アメル」", "ｽﾙﾋﾟﾘﾄﾞ");
        Check("ロキソプロフェンNa錠60mg「サワイ」", "ﾛｷｿﾌﾟﾛﾌｪﾝNa");
        Check("カロナール錠２００ ２００ｍｇ", "ｶﾛﾅｰﾙ");
        Check("カロナール細粒20%", "ｶﾛﾅｰﾙ");
        Check("ビオスリー配合錠", "ﾋﾞｵｽﾘｰ");
        Check("ケイラーゼＳＡ配合顆粒", "ｹｲﾗｰｾﾞSA");
        Check("リンデロン-VG軟膏0.12%", "ﾘﾝﾃﾞﾛﾝ-VG");
        Check("ポタコールＲ輸液 ２５０ｍＬ", "ﾎﾟﾀｺｰﾙR");
        Check("アセリオ静注液１０００ｍｇバッグ １，０００ｍｇ１００ｍＬ", "ｱｾﾘｵ");
        Check("レボフロキサシン錠500mg「サワイ」 レボフロキサシンとして", "ﾚﾎﾞﾌﾛｷｻｼﾝ");
        Check("レルベア200エリプタ30吸入用", "ﾚﾙﾍﾞｱ");
        Check("シムビコートタービュヘイラー60吸入", "ｼﾑﾋﾞｺｰﾄ");
        Check("アドエア250ディスカス60吸入用", "ｱﾄﾞｴｱ");
        Check("葛根湯エキス顆粒（医療用）", "葛根湯エキス", false);
        Check("ビタミンB12", "ﾋﾞﾀﾐﾝB12");
        Check("正露丸", "正露丸");
        Check("五苓散", "五苓散");
        Check("六君子湯", "六君子湯");
        Check("未知の薬ABC123", "未知の薬ABC123");
        Check(null, "");
        Check("　 ", "");
        Console.WriteLine("PASS: " + assertions + " name cases (including idempotency)");
        return 0;
    }
}

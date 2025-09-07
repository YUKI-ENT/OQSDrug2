# OQSDrug2 ： オンライン資格確認 xml薬剤情報取得表示ソフト <<薬剤情報、AI機能(ollama)強化版>>

## 概要

[OQSDrug version1](https://github.com/YUKI-ENT/OQSDrug)の後継バージョンです。
主な変更点としては、
- バックエンドデータベースをAccess mdb形式からPostgreSQLに変更
- 薬剤添付文書情報を持たせており、薬剤相互作用情報（併用禁忌や注意）のリスト形式表示が可能
- **AI機能**：ローカルLLMである`ollama`が使える環境では、xml薬歴取得後、**自動で投薬内容から疾患状況を推論して表示**可能
- 薬剤添付文書も本ツールから表示可能
- 添付文書情報もPostgreSQLに構造化した状態（JSON形式）で保存してありますので、今後LLMの学習等に利用加工が可能
  
となっております。

|   環境      | PostgreSQLなし      | PostgreSQLありOllamaなし | PostgreSQLあり、Ollamaあり                 |
|---------------|---------------------|-----------------------|-----------------------------|
| xml薬歴健診歴表示  | △(動作保証外)   | ⭕️                    | ⭕️                      |
| 薬剤添付文書表示 | ❌️                |  ⭕️                  |  ⭕️                     |
| 相互作用表示    | ❌️                |  ⭕️                  |  ⭕️                     |
| AI要約表示     | ❌️                |  ❌️                  |  ⭕️                     |



逆にVersion1からは、**PDF形式でのファイリング機能は削除しました**。引き続きPDF形式で薬歴を見たい方は、RSBaseやOQSDrug version1系統を利用するようにお願いします。

<<相互作用表示画面>>
![DI2](https://github.com/user-attachments/assets/c47418ce-bf79-4eee-94c1-7367034a4381)

<<AI推論表示画面>>
![DI3](https://github.com/user-attachments/assets/a66d916c-3d1b-4d73-943d-30db991f9952)

 

---

## 動作条件
- **PostgreSQL**
  - ある程度は従来のAccess mdb形式(OQSDrug_data.mdb)でも動作しますが、**薬剤併用情報やAI機能はPostgreSQL使用時しか動作しない**ようにしてあります。
  - PostgreSQLのバージョンは**15以上**が推奨です。RSBaseのPostgreSQLサーバーにデータを置くこともできるかもしれませんが、バージョンが古いPostgreSQLですとJSONB形式の保存や検索ができない可能性があるので、できれば別サーバーで新しいバージョンのものを入れてください。
  [PostgreSQLダウンロードサイト](https://www.postgresql.jp/download) からサーバーOSにあったものをダウンロードしてインストールしてください。
  - 設定はデフォルトで動くと思いますが、Windowsではインストール時に管理者(postgres)パスワードの設定、ディスク領域の余裕あるドライブにデータベース保存場所の選択はしておいてください。
  - 外部からアクセス可能にする設定：`pg_hba.conf` **の追記、Windowsファイヤーウォールのポート5432の開放**は必要です。
    
- **Ollama (Local LLMランタイム)**
  - Ollamaが利用できる環境では、**病名病態等の推論機能**が利用できます
  - 病名の推論では、`gemma3:12b`、`gpt-oss:20b` あたりが安定した出力でした。これらのモデルを動かすには、16GB以上のVRAMを積んだNvidia製GPUが必要です。当方では、**CPU Intel Corei9-9900、メモリ32GB、GeForce RTX5080 16GB/ Ubuntu24.04の環境**で安定動作を確認しました。
  - Windows、Macでも動作するようですので、[ダウンロードサイト](https://ollama.com/download/windows) からインストールしてください。
  - OllamaもLAN内のPCからアクセスできるよう設定が必要です。
    
- **ダイナミクス**  
  ダイナミクスが本ソフトの必須環境です。

- **RSBase**  
  RSBaseがあればRSBaseの薬剤情報詳細を表示できます

### 実行環境
- .NET 4.8Frameworkが必要です。最近のWindows10以上ではデフォルトでインストールされていますが、起動できない場合、以下のリンクから **.NET Framework 4.8 ランタイム** をダウンロードしてください：  
[公式ダウンロードページ](https://dotnet.microsoft.com/ja-jp/download/dotnet-framework/net48)
 
- **Accessデータベースエンジン**  
  実行PCにAccessデータベースエンジンが必要です。
  - **Access 32bit版** のインストール
  - または **Access ランタイム 32bit版** のインストール
   ( [公式ダウンロードページ](https://www.microsoft.com/ja-jp/download/details.aspx?id=50040) )
    で導入されます。

---

## 設置方法

1. [Release一覧](https://github.com/YUKI-ENT/OQSDrug2/releases)から**OQSDrug2_v2.xx.xx.xx.exe(インストーラになってます) をダウンロード**
2. 実行するとインストールが始まります。
3. <<**初回または薬剤情報バージョンアップ時のみ**>>`DrugSGMLdata_xxxxxxxx.backup` をダウンロードしてインポート作業を行ってください。
---

## 運用方法
運用方法は、[Version1のとき](https://github.com/YUKI-ENT/OQSDrug?tab=readme-ov-file#%E9%81%8B%E7%94%A8%E6%96%B9%E6%B3%95)と同じく設置してください。


## 設定と動作の説明

PostgreSQLの設定と相互作用、AI機能以外の基本機能は[Version1](https://github.com/YUKI-ENT/OQSDrug?tab=readme-ov-file#%E8%A8%AD%E5%AE%9A%E3%81%A8%E5%8B%95%E4%BD%9C%E3%81%AE%E8%AA%AC%E6%98%8E)と同じですので、新機能の部分のみ説明します。

- **初期設定**
   
   - 左側のStatusインジケータ部分に現在の状態が表示されます。設定でデータベース形式を変更すると、mdb/PostgreSQLの表示が変わります。
     ![top2](https://github.com/user-attachments/assets/35b5e62e-cfa8-4851-8dac-891a1cff562c)
   - 右上の「`設定`」を開きます
   - OQSDrug version1の設定ファイルも読み込めますので、まずversion1で設定の`エクスポート`をしてから、そのconfigファイルをインポートすると、医療機関コード等の設定がそのまま移行できます。
   - `①データベース形式`で、`PostgreSQL`を選択後、 `サーバーアドレス`、`Port`(デフォルト5432推奨)、`ユーザー名`(デフォルトpostgres推奨)、`パスワード`(PostgreSQLインストール時に設定してください) を設定
   - 「象のマークのついた`設定`」ボタンを押す

     ![settings11](https://github.com/user-attachments/assets/194cfbd0-7852-48ce-89f4-40413183760a)

   - PostgreSQLの設定画面になります。左上の部分が接続状況を示します。初期状態では以下のようになるはずです。

     ![PG11](https://github.com/user-attachments/assets/b226eac0-df0f-4319-9767-aba804834dcf)

   - 「`データベース/テーブル新規作成`」ボタンを押します。
   - 成功すると以下のように、`OQSDrug_data:Ready`が点灯します。
 
     ![PG12](https://github.com/user-attachments/assets/57a35355-d605-479c-b745-b7d419322b15)

   - 従来の `OQSDrugdata.mdb` を引き継ぐ時は、`移行元mdb`を選択後、`データ移行開始` ボタンを押してください。数分かかることもありますが、成功すると以下のようになります。引き継がずに新規に使用する場合はこのステップは不要です。
 
     ![PG13](https://github.com/user-attachments/assets/ca058531-fa53-4306-b2fb-d4e8ea2dff50)

   - **薬剤情報データベースをインポートします** [Release一覧](https://github.com/YUKI-ENT/OQSDrug2/releases) から`DrugSGMLdatayyyymmdd.backup`(約200MBあります)をダウンロード後、`Backupデータのインポート`ボタンを押して、このファイルを選択してください。インポートには1-5分程度かかります。インポート中は以下のような表示になり、フリーズしたみたいに見えますが、完了するまで操作しないようお願いします。

    ![PG14](https://github.com/user-attachments/assets/5a3096d1-61ec-460a-b180-0945279a154c)

   - 完了すると以下のようになります。これで初期設定は完了です。Version1と同じように操作できます。

    ![PG15](https://github.com/user-attachments/assets/43d4fbec-60fe-45ae-8687-3df66592fbfb)

   - **バックアップ機能** `保存先`のフォルダを選択後、 `バックアップ`ボタンを押すと、薬歴、健診歴、取得歴ログなどのテーブルを外部ファイルに手動保存します。
      `自動`にチェックをいれると、3時間毎に自動で指定フォルダにバックアップ作成します。7日経過すると削除される設定です。
     
     ![PG16](https://github.com/user-attachments/assets/e44a9507-e1ad-429e-a3c7-ec4158029414)


- **相互作用表示**
  - PostgreSQLモードにすると、`薬歴表示`をしたときに以下のように`相互作用`、`(AI)病態背景 ` のタブが追加されます。
    ![DI5](https://github.com/user-attachments/assets/a85bcf15-7cb8-4d36-9de8-25de023ac3f1)
  - `相互作用` タブを押すと、他院投薬中の薬剤に対する併用禁忌、注意の薬剤をリスト表示します。RSBaseがインストールされていれば、`相互作用相手`列の薬剤ダブルクリックで、その薬剤の添付文書をひらきますが、名前が一致しないと該当なしになることもあります。
  
    ![DI6](https://github.com/user-attachments/assets/cc4cf392-81e4-4cb9-a6fd-712931a1aaec)

  - 薬歴、相互作用どちらでも薬剤右クリックで、添付文書を開くことができます。また、薬剤名をダブルクリックした場合、`設定` → `Viewer設定`で`⑮添付文書表示選択` で選択したほうで添付文書を検索表示します。

    ![DI21](https://github.com/user-attachments/assets/9e6e7875-5fbc-4fc8-a69e-67c79e99ef92)

    ![DI22](https://github.com/user-attachments/assets/c83ecddf-64d8-4c0d-9b3d-5336f5b6bcd5)

    こんな感じの薬剤情報が表示できます。

    ![DI23](https://github.com/user-attachments/assets/29d3b88d-42d4-44f1-8cfc-bae95d8cbfb8)

    詳細は[こちら](https://github.com/YUKI-ENT/OQSDrug2/releases/tag/v2.25.9.3)。



- **AI推論表示**
  - `(AI)病態背景`は`Ollama`の設定がされている場合機能します。
  - もし、ローカルLLMをつかわず、ChatGPTなど外部汎用LLMを使う場合は、①のテンプレート選択後③のリサイクルマークのボタンを押すとプロンプトを⑥のテキストボックスに個人情報は削除した状態で生成しますので、これをコピーペーストして利用してもよいかと思いますが、汎用AI利用のプライバシーやガイドラインリスクには十分注意ください。
  - ローカルLLMの場合はプロンプト生成後⑤`送信`を押すとAI推論を開始します。数秒から数十秒後に返信が返ってくると⑦のテキストボックスに推論内容が表示されます。
    ![DI7](https://github.com/user-attachments/assets/81cf5ae5-8301-4fb0-be78-36e9a5bf1ac4)
  - **取り込み操作を行うPCの**`設定`で`AI自動検索` を設定したうえで、以下のテンプレート設定で`自動取得` をチェックすると、xml取得後そのテンプレートでプロンプト作成からLLM問い合わせまで自動実行します。履歴はコンボボックスにリスト表示されます。
- **テンプレート編集**
  - ②の鉛筆マークのボタンを押すと、AI問い合わせ用のテンプレート編集画面が開きます。
    ![AI1](https://github.com/user-attachments/assets/5f2f7bdc-f006-4e82-90d9-36e0838c9d09)
  - いくつかプリセットで用意していますが、適宜プロンプト内容など変えてみて変化をみてみてください。ここで`自動取得`にチェックが入ったテンプレートについて、全体設定の`AI自動検索`もチェックされている自動でAI問い合わせを行いますが、あまりたくさんのテンプレートで行うと処理が溜まって回答に時間がかかるようになるので、自動をつけるのはテンプレート一つくらいにしておいてください。 


## 注意事項と今後の展望
- 薬剤添付文書はPMDAのダウンロードサービスから取得しておりますので、二次配布はお控えください。
- AIプロンプト作成は、固定指示部分と、データぺーロード部分にわかれています。薬剤投与データからスクリプトで慢性急性をスコア化し、その上位のものを選択してペイロードに乗せるようにしているので、臨時的に出された薬や途中で用量変更になった場合などは別処方と認識されて漏れてしまう可能性もあります。あくまで参考程度にとどめてください。
- ローカルLLMは汎用AIに比べて学習量が少ないので、薬剤→病名の関連付けには薬剤添付文書データを利用し、これもペイロードに乗せることで回答の精度をあげています。汎用AI利用時は、`ペイロード`の設定で、`薬効分類` `適応症` のチェックを外してプロンプト作成したほうが回答精度が上がるかもしれません。
- 現在添付文書データの`相互作用` `適応症`などごく一部のデータしか利用していませんが、今後他の項目の表示機能も考えていきたいと思います。ご要望ありましたらDiscussionから提案お願いします。

using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

//////////////////////////////////
/**
 * Formクラスを拡張してプログラムとする。
 * 複数の画像をひとつのウィンドウに表示するプログラム。
 */
class MultiImagesViewer : Form{
	////////////////////////////////////////////////////////////////////
	//フィールド
	////////////////////////////////////////////////////////////////////
	//メニュー
	private MenuStrip my_menu;
	//メニュー項目
	private ToolStripMenuItem[] my_menu_item = new ToolStripMenuItem[4];

	//画像を表示するためのピクチャーボックス
	private List<PictureBox> my_picture_boxes;

	//画像の同時表示数
	private int num_show_images = 3;
	//画像の同時表示数の最大値
	private int max_num_show_images = 4;

	//画像の同時表示数のプロパティ
	private int NumShowImages{
		get{ return this.num_show_images;  }
		set{ this.num_show_images = value; }
	}//property

	//表示中の画像（配列内の番号 複数表示しているひとつめのもの 配列のキーではなく「nページ目」という整数のもの）
	private int now_showing_images_order = 1;
	//表示中の画像のプロパティ
	private int NowShowingImagesOrder{
		get{ return this.num_show_images;  }
		set{ this.num_show_images = value; }
	}//property

	//表示画像のファイルパス
	private string[] path_image_files;

	//表示画像のディレクトリパス
	private string path_image_directory = "";


	////////////////////////////////////////////////////////////////////
	//メソッド
	////////////////////////////////////////////////////////////////////

	//////////////////////////////////
	/**
	 * 実行メソッド
	 * メモ：ドラッグアンドドロップを有効にするにはMainメソッドに[STAThread]が必要になるらしい。
	 */
	[STAThread]
	public static void Main(){
		Application.Run(new MultiImagesViewer());
	}//function


	//////////////////////////////////
	/**
	 * コンストラクタ。
	 * コンストラクタには戻り値を設定しない。
	 */
	public MultiImagesViewer(){
		//この場合の this は継承したFormクラス
		this.Text = "複数画像ビューアー";
		this.Width  = 1000;
		this.Height =  600;

		////////////////////////////////
		//メニュー
		this.InitiateMenu();

		////////////////////////////////
		//キー入力のイベントハンドラを登録する
		this.KeyDown += new KeyEventHandler(FormOnKeyDown);

		////////////////////////////////
		//表示すべき画像のパスの配列 初期化
		this.path_image_files = this.GetPathImageFiles(this.path_image_directory);

		////////////////////////////////
		//画像表示のための初期化
		this.InitiateImages(this.num_show_images);

		//画像割り当て
		this.ApplyImages(
			this.path_image_files,
			this.num_show_images,
			this.now_showing_images_order
		);

		////////////////////////////////
		//ウィンドウリサイズ時の挙動
		this.Resize += new EventHandler(this.DoWhenResize);

	}//function


	//////////////////////////////////
	/**
	 * メニューの設定。
	 */
	public void InitiateMenu(){
		//メニューの枠組み
		this.my_menu = new MenuStrip();

		//階層構造を考慮せず、メニュー項目をすべて作成
		this.my_menu_item[0] = new ToolStripMenuItem("表示数設定");
		this.my_menu_item[1] = new ToolStripMenuItem("2");
		this.my_menu_item[2] = new ToolStripMenuItem("3");
		this.my_menu_item[3] = new ToolStripMenuItem("4");

		//メニュー項目に親子関係を付ける
		this.my_menu_item[0].DropDownItems.Add(this.my_menu_item[1]);
		this.my_menu_item[0].DropDownItems.Add(this.my_menu_item[2]);
		this.my_menu_item[0].DropDownItems.Add(this.my_menu_item[3]);

		//メイン項目となる要素を枠組みに設定
		this.my_menu.Items.Add(this.my_menu_item[0]);
		this.MainMenuStrip = this.my_menu;

		this.my_menu.Parent = this;

		//メニュー選択時の挙動を設定
		for(int i = 1; i <= 3; i++){
			this.my_menu_item[i].Click += new EventHandler(SelectNumOfShowingImage);
		}//for
	}//function


	//////////////////////////////////
	/**
	 * 画像表示の枠組みの初期化。
	 * 表示数の設定。
	 * 
	 * @param  画像表示数
	 */
	public void InitiateImages(int num_show_images){
		Debug.WriteLine("ピクチャーボックス初期化開始");

		//画像表示枠
		//初期化のたびに再生成されるのを防ぐ
		if(this.my_picture_boxes == null){
			this.my_picture_boxes = new List<PictureBox>();

			//画像の枠の数だけ繰り返し処理
			//右から順に表示したい。[3 2 1]みたいに
			for(int i = 0; i < this.max_num_show_images; i++){
				this.my_picture_boxes.Add(new PictureBox());
			}//for

			foreach(PictureBox now_picture_box in this.my_picture_boxes){

				//画像の大きさをPictureBoxに合わせる
				now_picture_box.SizeMode = PictureBoxSizeMode.Zoom;

				//位置指定
				now_picture_box.Top  = 0;

				////////////////////////////////
				//ドラッグドロップ時の挙動 ファイルパス/ディレクトリパスを受け取る
				//ドラッグドロップを許可
				now_picture_box.AllowDrop = true;
				now_picture_box.DragEnter += new DragEventHandler(this.DoWhenDragEnter);
				now_picture_box.DragDrop  += new DragEventHandler(this.DoWhenDragDrop);

				now_picture_box.Parent = this;
			}//for
		}//if

		//画像の枠の数だけ繰り返し処理
		//表示数やウィンドウの大きさの変更時にも処理される
		//右から順に表示したい。[3 2 1]みたいに
		for(int i = 0; i < num_show_images; i++){

			this.my_picture_boxes[i].Width  = this.ClientSize.Width / num_show_images;
			//表示数設定ボタンの領域分だけ高さを調整
			this.my_picture_boxes[i].Height = this.ClientSize.Height;

			//位置指定
			this.my_picture_boxes[i].Top  = 0;
			this.my_picture_boxes[i].Left = (this.ClientSize.Width / num_show_images) * ((num_show_images -1) - i); //配列が0で始まる分、1を余計に引く

		}//for

		Debug.WriteLine("ピクチャーボックス初期化終了");
	}//function


	//////////////////////////////////
	/**
	 * 指定ディレクトリ以下の画像ファイルのパスのリストを
	 * 取得する。
	 * 
	 * @param  指定ディレクトリ
	 * @return 画像パスの配列
	 */
	public string[] GetPathImageFiles(string path_directory){
		List<String> path_list = new List<String>();

		//ディレクトリ指定があれば
		if(path_directory.Equals(string.Empty) == false){
			//指定ディレクトリ以下のファイルをすべて取得する
			//ワイルドカード"*"は、すべてのファイルを意味する
			string[] files = Directory.GetFiles(
				@path_directory,
				"*",
				System.IO.SearchOption.AllDirectories
			);

			//画像だけ取り出す
			foreach(string now_path in files){
				if(
					(now_path.IndexOf(".jpg")  > 0)||
					(now_path.IndexOf(".png")  > 0)||
					(now_path.IndexOf(".jpeg") > 0)||
					(now_path.IndexOf(".bmp")  > 0)||
					(now_path.IndexOf(".gif")  > 0)
				){
					path_list.Add(now_path);
				}//if
			}//foreach
		}//if


		//Listを配列に変換
		string[] path_list_array = path_list.ToArray();

		//確認出力
		Debug.WriteLine("画像パスの数 : " + path_list_array.Length);

		return path_list_array;
	}//function


	//////////////////////////////////
	/**
	 * 画像の割り当て。
	 * 
	 * @param  画像パスの配列
	 * @param  画像の表示数
	 * @param  画像配列内の表示開始位置
	 */
	public void ApplyImages(
		string[] path_image_files,
		int      num_show_images,
		int      now_showing_images_order
	){
		Debug.WriteLine("path_image_files.Length : " + path_image_files.Length);
		Debug.WriteLine("現在の表示先頭位置" + now_showing_images_order);

		//ディレクトリ指定があれば
		if(path_image_files.Length > 0){
			//画像の枠の数だけ繰り返し処理
			for(int i = 0; i < num_show_images; i++){
				//画像配列内での表示位置（配列のキーではなく、1から始まる「nページ目」という数字）
				int order_to_show = now_showing_images_order + i;
				//画像パス配列の要素数より大きくなったら（最後の画像が表示された次のループなら）、配列範囲内に戻す
				if(order_to_show > path_image_files.Length){
					order_to_show -= path_image_files.Length;
				}//if

				//ページ番号を配列のキーに変換
				int order_to_sho_in_array = order_to_show - 1;

				//ロードすべき画像パス
				string now_image_path_to_show = path_image_files[order_to_sho_in_array];
				this.my_picture_boxes[i].Load(now_image_path_to_show);
			}//for
		}//if
	}//function


	//////////////////////////////////
	/**
	 * フォームのキー入力リスナー
	 */
	public void FormOnKeyDown(
		Object sender,
		KeyEventArgs e
	){
		String input_key = "";

		//入力されたキーを e.KeyCode で取得
		switch(e.KeyCode){
			case Keys.Up:
				input_key = "上";
				break;

			case Keys.Right:
				input_key = "右";

				//表示画像をひとつ戻らせる
				this.now_showing_images_order--;
				//表示開始位置が0＝先頭の画像から末尾の画像に移動
				if(this.now_showing_images_order == 0){
					this.now_showing_images_order = this.path_image_files.Length;
				}//if
				this.ApplyImages(
					this.path_image_files,
					this.num_show_images,
					this.now_showing_images_order
				);
				break;

			case Keys.Left:
				input_key = "左";
				//表示画像をひとつ進ませる
				this.now_showing_images_order++;
				//表示開始位置が配列サイズより大きい＝末尾の画像から先頭の画像に移動
				if(this.now_showing_images_order > this.path_image_files.Length){
					this.now_showing_images_order = 1;
				}//if
				this.ApplyImages(
					this.path_image_files,
					this.num_show_images,
					this.now_showing_images_order
				);
				break;

			case Keys.Down:
				input_key = "下";
				break;

			default:
				input_key = "矢印以外";
				break;
		}//switch

		//押されたキーの確認
		Debug.WriteLine("入力キー : " + input_key);
	}//function


	//////////////////////////////////
	/**
	 * ウィンドウの大きさが変更された場合のリスナー
	 */
	private void DoWhenResize(
		object sender,
		EventArgs e
	){
		Debug.WriteLine("this.Width  : " + this.Width);
		Debug.WriteLine("this.Height : " + this.Height);

		//画像表示のための初期化
		this.InitiateImages(this.num_show_images);

		//画像割り当て
		this.ApplyImages(
			this.path_image_files,
			this.num_show_images,
			this.now_showing_images_order
		);

	}//function


	//////////////////////////////////
	/**
	 * ドラッグドロップ操作のリスナー。
	 * フォームにマウスが重なったとき。
	 * 
	 * ドラッグアンドドロップ時のカーソル表示を制御する。
	 */
	private void DoWhenDragEnter(
		object sender,
		DragEventArgs e
	){
		//すべてのドラッグドロップを有効にする
		e.Effect = DragDropEffects.All;
	}//function


	//////////////////////////////////
	/**
	 * ドラッグドロップ操作のリスナー
	 */
	private void DoWhenDragDrop(
		object sender,
		DragEventArgs e
	){
		string file_path_to_open = ""; //ドロップされたファイルのパス
		string dir_path_to_open  = ""; //ドロップされたファイルが属するディレクトリのパス

		if(e.Data.GetDataPresent(DataFormats.FileDrop)){
			string[] file_path_to_open_array = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			file_path_to_open = file_path_to_open_array[0];
			dir_path_to_open  = Path.GetDirectoryName(@file_path_to_open);

			//画像パスの配列を、ドラッグドロップされたファイルのディレクトリ内の画像一覧に更新
			this.path_image_files = this.GetPathImageFiles(dir_path_to_open);
			Debug.WriteLine(file_path_to_open);

			//表示ページ位置を先頭に
			this.now_showing_images_order = 1;

			//画像割り当て
			this.ApplyImages(
				this.path_image_files,
				this.num_show_images,
				this.now_showing_images_order
			);

			//現在地表示テキストボックス更新
			this.path_image_directory = dir_path_to_open;
			this.Text = this.path_image_directory;

		}//if

	}//function


	//////////////////////////////////
	/**
	 * メニュー選択時の挙動。
	 * 画像表示数を設定
	 */
	private void SelectNumOfShowingImage(
		object sender,
		EventArgs e
	){
		//選択された項目を取り出す
		ToolStripMenuItem mi = (ToolStripMenuItem)sender;
		this.num_show_images = int.Parse(mi.Text);

		//画像表示領域を初期化
		this.InitiateImages(this.num_show_images);
	}//function
}//class

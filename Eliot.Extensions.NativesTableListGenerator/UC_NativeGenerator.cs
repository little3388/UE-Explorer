﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UEExplorer.UI.Tabs;
using UELib;
using UELib.Core;
using UELib.Flags;
using UEExplorer.Development;
using UEExplorer.UI;

namespace Eliot.Extensions.NativesTableListGenerator
{
	[System.Runtime.InteropServices.ComVisible( false )]
	public partial class UC_NativeGenerator : UserControl_Tab
	{
		private readonly NativesTablePackage _NTLPackage = new NativesTablePackage();

		public UC_NativeGenerator()
		{
			InitializeComponent();

			_NTLPackage.NativeTableList = new List<NativeTableItem>();
		}

		private void Button_Add_Click( object sender, EventArgs e )
		{
			var dialogResult = OpenNTLDialog.ShowDialog( this );
			if( dialogResult != DialogResult.OK )
			{
				return;
			}

			var packages = new List<UnrealPackage>();
			foreach( var fileName in OpenNTLDialog.FileNames )
			{
				packages.Add( UnrealLoader.LoadPackage( fileName ) );
			}

			if( packages.Count > 0 )
			{
				FileNameTextBox.Enabled = true;
				Button_Save.Enabled = true;
			}
	
			TreeView_Packages.BeginUpdate();
			foreach( var package in packages )
			{
				package.RegisterClass( "Function", typeof(UFunction) );
				package.InitializeExportObjects( UnrealPackage.InitFlags.Deserialize );

				foreach( var function in package.ObjectsList.OfType<UFunction>() )
				{
					if( !function.HasFunctionFlag( FunctionFlags.Native ) || function.NativeToken == 0 ) 
						continue;

					var item = new NativeTableItem
					{
						Name = function.FriendlyName,
						OperPrecedence = function.OperPrecedence,
						ByteToken = function.NativeToken
					};
					item.InitializeType( function );
	
					var packageNode = TreeView_Packages.Nodes.Add( package.PackageName );
					var itemNode = packageNode.Nodes.Add( item.Name );
					itemNode.Nodes.Add( "Format:" + item.Type );
					itemNode.Nodes.Add( "ByteToken:" + item.ByteToken );
					itemNode.Nodes.Add( "OperPrecedence:" + item.OperPrecedence );

					_NTLPackage.NativeTableList.Add( item );
				}
				TreeView_Packages.Refresh();
				package.Stream.Close();
			}
			TreeView_Packages.EndUpdate();
		}

		private void Button_Save_Click( object sender, EventArgs e )
		{
			_NTLPackage.CreatePackage
			( 
				Path.Combine
				( 
					Application.StartupPath, 
					"Native Tables", 
					"NativesTableList_" + FileNameTextBox.Text 
				) 
			);
		}
	} 

	[ExtensionTitle( "NTL Generator" )]
	public class ExtNativeGen : IExtension
	{
		private ProgramForm _Owner;

		/// <summary>
		/// Called after UEExplorer_Form is initialized.
		/// </summary>
		/// <param name="form"></param>
		public void Initialize( ProgramForm form )
		{
			_Owner = form;
		}

		/// <summary>
		/// Called when activated by end-user.
		/// </summary>
		public void OnActivate( object sender, EventArgs e )
		{
			_Owner.TManager.AddTabComponent( typeof(UC_NativeGenerator), "Natives Table List Generator" );
		}
	}
}
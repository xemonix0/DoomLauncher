﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoomLauncher.Interfaces
{
    public interface IDataSourceAdapter : IGameFileDataSourceAdapter, IIWadDataSourceAdapter, IConfigurationDataSourceAdapter, IStatsDataSourceAdapter
    {
        void UpdateGameFiles(GameFileFieldType ftWhere, GameFileFieldType ftSet, object fWhere, object fSet);

        IEnumerable<ISourcePortData> GetSourcePorts();
        IEnumerable<ISourcePortData> GetUtilities();
        ISourcePortData GetSourcePort(int sourcePortID);
        void InsertSourcePort(ISourcePortData sourcePort);
        void UpdateSourcePort(ISourcePortData sourcePort);
        void DeleteSourcePort(ISourcePortData sourcePort);

        IEnumerable<IFileData> GetFiles(IGameFile gameFile);
        IEnumerable<IFileData> GetFiles(IGameFile gameFile, FileType fileTypeID);
        void UpdateFile(IFileData file);
        void UpdateFiles(int sourcePortID_Where, int? sourcePortID_Set);
        void InsertFile(IFileData file);
        void DeleteFile(IFileData file);
        void DeleteFile(IGameFile file);

        IEnumerable<IGameFile> GetGameFiles(ITagData tag);
        IEnumerable<IGameFile> GetGameFiles(IGameFileGetOptions options, ITagData tag);

        //IEnumerable<ICustomParamDataSource> GetCustomParameters(int gameFileID);
        //void InsertCustomParameter(ICustomParamDataSource data);
        //void DeleteCustomParameter(ICustomParamDataSource data);
    }
}

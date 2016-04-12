﻿using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using NPoco;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.EntityBase;
using Umbraco.Core.Persistence.Mappers;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Core.Persistence.UnitOfWork;

namespace Umbraco.Core.Persistence.Repositories
{
    /// <summary>
    /// Represent an abstract Repository for NPoco based repositories
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    internal abstract class NPocoRepositoryBase<TId, TEntity> : RepositoryBase<TId, TEntity>
        where TEntity : class, IAggregateRoot
    {
        public ISqlSyntaxProvider SqlSyntax { get; }

        /// <summary>
        /// Returns the Query factory
        /// </summary>
        public override QueryFactory QueryFactory { get; }

        /// <summary>
        /// Used to create a new query instance
        /// </summary>
        /// <returns></returns>
        public override Query<TEntity> Query => QueryFactory.Create<TEntity>();

        protected NPocoRepositoryBase(IUnitOfWork work, CacheHelper cache, ILogger logger, ISqlSyntaxProvider sqlSyntax, IMappingResolver mappingResolver)
            : base(work, cache, logger)
        {
            if (sqlSyntax == null) throw new ArgumentNullException(nameof(sqlSyntax));
            SqlSyntax = sqlSyntax;
            QueryFactory = new QueryFactory(SqlSyntax, mappingResolver);
        }

        /// <summary>
		/// Returns the database Unit of Work added to the repository
		/// </summary>
		protected internal new IDatabaseUnitOfWork UnitOfWork => (IDatabaseUnitOfWork) base.UnitOfWork;

        protected UmbracoDatabase Database => UnitOfWork.Database;

        protected Sql<SqlContext> Sql()
        {
            return NPoco.Sql.BuilderFor(new SqlContext(SqlSyntax, Database));
        }

        #region Abstract Methods

        protected abstract Sql<SqlContext> GetBaseQuery(bool isCount);
        protected abstract string GetBaseWhereClause();
        protected abstract IEnumerable<string> GetDeleteClauses();
        protected abstract Guid NodeObjectTypeId { get; }
        protected abstract override void PersistNewItem(TEntity entity);
        protected abstract override void PersistUpdatedItem(TEntity entity);

        #endregion

        protected override bool PerformExists(TId id)
        {
            var sql = GetBaseQuery(true);
            sql.Where(GetBaseWhereClause(), new { Id = id});
            var count = Database.ExecuteScalar<int>(sql);
            return count == 1;
        }

        protected override int PerformCount(IQuery<TEntity> query)
        {
            var sqlClause = GetBaseQuery(true);
            var translator = new SqlTranslator<TEntity>(sqlClause, query);
            var sql = translator.Translate();

            return Database.ExecuteScalar<int>(sql);
        }

        protected override void PersistDeletedItem(TEntity entity)
        {
            var deletes = GetDeleteClauses();
            foreach (var delete in deletes)
            {
                Database.Execute(delete, new { Id = GetEntityId(entity) });
            }
        }

        protected virtual new TId GetEntityId(TEntity entity)
        {
            return (TId)(object)entity.Id;
        }
    }
}
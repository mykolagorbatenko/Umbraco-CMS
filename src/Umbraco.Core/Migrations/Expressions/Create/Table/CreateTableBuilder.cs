﻿using System.Data;
using NPoco;
using Umbraco.Core.Migrations.Expressions.Common.Expressions;
using Umbraco.Core.Migrations.Expressions.Create.Expressions;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;

namespace Umbraco.Core.Migrations.Expressions.Create.Table
{
    public class CreateTableBuilder : ExpressionBuilderBase<CreateTableExpression, ICreateTableColumnOptionBuilder>,
                                                ICreateTableWithColumnBuilder,
                                                ICreateTableColumnAsTypeSyntax,
                                                ICreateTableColumnOptionForeignKeyCascadeBuilder
    {
        private readonly IMigrationContext _context;
        private readonly DatabaseType[] _supportedDatabaseTypes;

        public CreateTableBuilder(IMigrationContext context, DatabaseType[] supportedDatabaseTypes, CreateTableExpression expression)
            : base(expression)
        {
            _context = context;
            _supportedDatabaseTypes = supportedDatabaseTypes;
        }

        public ColumnDefinition CurrentColumn { get; set; }

        public ForeignKeyDefinition CurrentForeignKey { get; set; }

        public override ColumnDefinition GetColumnForType()
        {
            return CurrentColumn;
        }

        public ICreateTableColumnAsTypeSyntax WithColumn(string name)
        {
            var column = new ColumnDefinition { Name = name, TableName = Expression.TableName, ModificationType = ModificationType.Create };
            Expression.Columns.Add(column);
            CurrentColumn = column;
            return this;
        }

        public ICreateTableColumnOptionBuilder WithDefault(SystemMethods method)
        {
            CurrentColumn.DefaultValue = method;
            return this;
        }

        public ICreateTableColumnOptionBuilder WithDefaultValue(object value)
        {
            CurrentColumn.DefaultValue = value;
            return this;
        }

        public ICreateTableColumnOptionBuilder Identity()
        {
            CurrentColumn.IsIdentity = true;
            return this;
        }

        public ICreateTableColumnOptionBuilder Indexed()
        {
            return Indexed(null);
        }

        public ICreateTableColumnOptionBuilder Indexed(string indexName)
        {
            CurrentColumn.IsIndexed = true;

            var index = new CreateIndexExpression(_context, _supportedDatabaseTypes, new IndexDefinition
            {
                Name = indexName,
                SchemaName = Expression.SchemaName,
                TableName = Expression.TableName
            });

            index.Index.Columns.Add(new IndexColumnDefinition
                                        {
                                            Name = CurrentColumn.Name
                                        });

            _context.Expressions.Add(index);

            return this;
        }

        public ICreateTableColumnOptionBuilder PrimaryKey()
        {
            CurrentColumn.IsPrimaryKey = true;

            //For MySQL, the PK will be created WITH the create table expression, however for
            // SQL Server, the PK get's created in a different Alter table expression afterwords.
            // MySQL will choke if the same constraint is again added afterword
            // TODO: This is a super hack, I'd rather not add another property like 'CreatesPkInCreateTableDefinition' to check
            // for this, but I don't see another way around. MySQL doesn't support checking for a constraint before creating
            // it... except in a very strange way but it doesn't actually provider error feedback if it doesn't work so we cannot use
            // it.  For now, this is what I'm doing
            if (Expression.DatabaseType.IsMySql() == false)
            {
                var expression = new CreateConstraintExpression(_context, _supportedDatabaseTypes, ConstraintType.PrimaryKey)
                {
                    Constraint =
                {
                    TableName = CurrentColumn.TableName,
                    Columns = new[] { CurrentColumn.Name }
                }
                };
                _context.Expressions.Add(expression);
            }

            return this;
        }

        public ICreateTableColumnOptionBuilder PrimaryKey(string primaryKeyName)
        {
            CurrentColumn.IsPrimaryKey = true;
            CurrentColumn.PrimaryKeyName = primaryKeyName;

            //For MySQL, the PK will be created WITH the create table expression, however for
            // SQL Server, the PK get's created in a different Alter table expression afterwords.
            // MySQL will choke if the same constraint is again added afterword
            // TODO: This is a super hack, I'd rather not add another property like 'CreatesPkInCreateTableDefinition' to check
            // for this, but I don't see another way around. MySQL doesn't support checking for a constraint before creating
            // it... except in a very strange way but it doesn't actually provider error feedback if it doesn't work so we cannot use
            // it.  For now, this is what I'm doing

            if (Expression.DatabaseType.IsMySql() == false)
            {
                var expression = new CreateConstraintExpression(_context, _supportedDatabaseTypes, ConstraintType.PrimaryKey)
                {
                    Constraint =
                {
                    ConstraintName = primaryKeyName,
                    TableName = CurrentColumn.TableName,
                    Columns = new[] { CurrentColumn.Name }
                }
                };
                _context.Expressions.Add(expression);
            }

            return this;
        }

        public ICreateTableColumnOptionBuilder Nullable()
        {
            CurrentColumn.IsNullable = true;
            return this;
        }

        public ICreateTableColumnOptionBuilder NotNullable()
        {
            CurrentColumn.IsNullable = false;
            return this;
        }

        public ICreateTableColumnOptionBuilder Unique()
        {
            return Unique(null);
        }

        public ICreateTableColumnOptionBuilder Unique(string indexName)
        {
            CurrentColumn.IsUnique = true;

            var index = new CreateIndexExpression(_context, _supportedDatabaseTypes, new IndexDefinition
            {
                Name = indexName,
                SchemaName = Expression.SchemaName,
                TableName = Expression.TableName,
                IsUnique = true
            });

            index.Index.Columns.Add(new IndexColumnDefinition
                                        {
                                            Name = CurrentColumn.Name
                                        });

            _context.Expressions.Add(index);

            return this;
        }

        public ICreateTableColumnOptionForeignKeyCascadeBuilder ForeignKey(string primaryTableName, string primaryColumnName)
        {
            return ForeignKey(null, null, primaryTableName, primaryColumnName);
        }

        public ICreateTableColumnOptionForeignKeyCascadeBuilder ForeignKey(string foreignKeyName, string primaryTableName,
                                                                          string primaryColumnName)
        {
            return ForeignKey(foreignKeyName, null, primaryTableName, primaryColumnName);
        }

        public ICreateTableColumnOptionForeignKeyCascadeBuilder ForeignKey(string foreignKeyName, string primaryTableSchema,
                                                                          string primaryTableName, string primaryColumnName)
        {
            CurrentColumn.IsForeignKey = true;

            var fk = new CreateForeignKeyExpression(_context, _supportedDatabaseTypes, new ForeignKeyDefinition
            {
                Name = foreignKeyName,
                PrimaryTable = primaryTableName,
                PrimaryTableSchema = primaryTableSchema,
                ForeignTable = Expression.TableName,
                ForeignTableSchema = Expression.SchemaName
            });

            fk.ForeignKey.PrimaryColumns.Add(primaryColumnName);
            fk.ForeignKey.ForeignColumns.Add(CurrentColumn.Name);

            _context.Expressions.Add(fk);
            CurrentForeignKey = fk.ForeignKey;
            return this;
        }

        public ICreateTableColumnOptionForeignKeyCascadeBuilder ForeignKey()
        {
            CurrentColumn.IsForeignKey = true;
            return this;
        }

        public ICreateTableColumnOptionForeignKeyCascadeBuilder ReferencedBy(string foreignTableName, string foreignColumnName)
        {
            return ReferencedBy(null, null, foreignTableName, foreignColumnName);
        }

        public ICreateTableColumnOptionForeignKeyCascadeBuilder ReferencedBy(string foreignKeyName, string foreignTableName,
                                                                            string foreignColumnName)
        {
            return ReferencedBy(foreignKeyName, null, foreignTableName, foreignColumnName);
        }

        public ICreateTableColumnOptionForeignKeyCascadeBuilder ReferencedBy(string foreignKeyName, string foreignTableSchema,
                                                                            string foreignTableName, string foreignColumnName)
        {
            var fk = new CreateForeignKeyExpression(_context, _supportedDatabaseTypes, new ForeignKeyDefinition
            {
                Name = foreignKeyName,
                PrimaryTable = Expression.TableName,
                PrimaryTableSchema = Expression.SchemaName,
                ForeignTable = foreignTableName,
                ForeignTableSchema = foreignTableSchema
            });

            fk.ForeignKey.PrimaryColumns.Add(CurrentColumn.Name);
            fk.ForeignKey.ForeignColumns.Add(foreignColumnName);

            _context.Expressions.Add(fk);
            CurrentForeignKey = fk.ForeignKey;
            return this;
        }

        public ICreateTableColumnOptionForeignKeyCascadeBuilder OnDelete(Rule rule)
        {
            CurrentForeignKey.OnDelete = rule;
            return this;
        }

        public ICreateTableColumnOptionForeignKeyCascadeBuilder OnUpdate(Rule rule)
        {
            CurrentForeignKey.OnUpdate = rule;
            return this;
        }

        public ICreateTableColumnOptionBuilder OnDeleteOrUpdate(Rule rule)
        {
            OnDelete(rule);
            OnUpdate(rule);
            return this;
        }
    }
}
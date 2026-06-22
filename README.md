# ai-issue-analysis

一个通用的 GitHub Issue AI 分析工具，使用 OpenAI 兼容 API（如 DeepSeek、OpenAI、Azure 等）自动分析 Issue 并生成结论。

## 功能特点

- 支持任何 OpenAI 兼容的 API（DeepSeek、OpenAI、Azure、本地模型等）
- 自动读取 Issue 正文和评论
- 生成结构化的分析报告
- 将分析结果回写到 Issue 评论
- 简单易用，无需复杂配置

## 快速接入

### 1. 配置 Secrets

在你的 GitHub 仓库 → Settings → Secrets and variables → Actions 中添加：

| Secret 名称 | 说明 | 示例值 |
|-------------|------|--------|
| `AI_API_KEY` | AI 服务的 API Key | `sk-xxx` |
| `AI_BASE_URL` | API 端点 URL | `https://api.deepseek.com` |
| `AI_MODEL` | 模型名称 | `deepseek-v4-flash` |

### 2. 复制文件

将以下文件复制到你的仓库：

```
.github/
├── workflows/
│   └── ai-issue-analysis.yml
└── scripts/
    └── analyze_issue.py
```

### 3. 测试

创建一个新 Issue，或在已有 Issue 中评论 `@github-actions`，即可触发 AI 分析。

## 支持的 AI 服务

### DeepSeek

```yaml
AI_BASE_URL: https://api.deepseek.com
AI_MODEL: deepseek-v4-flash
```

### OpenAI

```yaml
AI_BASE_URL: https://api.openai.com/v1
AI_MODEL: gpt-4o
```

### Azure OpenAI

```yaml
AI_BASE_URL: https://your-resource.openai.azure.com/
AI_MODEL: gpt-4o
```

### 本地模型（如 Ollama）

```yaml
AI_BASE_URL: http://localhost:11434/v1
AI_MODEL: llama3
```

## 工作流程

1. 用户创建 Issue 或评论 `@github-actions`
2. GitHub Actions 自动触发
3. Python 脚本读取 Issue 内容
4. 调用 AI API 生成分析
5. 将分析结果回写到 Issue 评论

## 自定义

### 修改分析提示词

编辑 `.github/scripts/analyze_issue.py` 中的 `prompt` 变量，可以自定义 AI 的分析行为。

### 添加项目特定知识

如果你的项目有特定的日志格式、错误模式或架构，可以在提示词中添加相关说明，提高分析准确性。

## 示例

查看 [zlqd123/zunmouse#5](https://github.com/zlqd123/zunmouse/issues/5) 的 AI 分析结果。

## 许可证

MIT License

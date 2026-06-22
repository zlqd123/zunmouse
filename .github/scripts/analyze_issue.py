import os
import requests
from openai import OpenAI

# Get environment variables
issue_number = os.environ["ISSUE_NUMBER"]
repo = os.environ["REPO"]
token = os.environ["GITHUB_TOKEN"]
comment_id = os.environ["COMMENT_ID"]
run_id = os.environ["RUN_ID"]

# GitHub API headers
headers = {
    "Authorization": f"token {token}",
    "Accept": "application/vnd.github.v3+json"
}

# Fetch issue
print(f"Fetching issue #{issue_number} from {repo}...")
issue_url = f"https://api.github.com/repos/{repo}/issues/{issue_number}"
issue_resp = requests.get(issue_url, headers=headers)
issue_data = issue_resp.json()

issue_title = issue_data.get("title", "")
issue_body = issue_data.get("body", "") or ""

# Fetch comments
comments_url = f"https://api.github.com/repos/{repo}/issues/{issue_number}/comments"
comments_resp = requests.get(comments_url, headers=headers)
comments_data = comments_resp.json()

comments_text = ""
for c in comments_data:
    author = c.get("user", {}).get("login", "unknown")
    body = c.get("body", "")
    comments_text += f"\n\n**@{author}:**\n{body}"

# Build prompt
prompt = f"""你是一个 GitHub Issue 分析助手。请分析以下 Issue 并给出结论。

## Issue #{issue_number}: {issue_title}

### 正文
{issue_body}

### 评论
{comments_text if comments_text else "无评论"}

---

请分析这个 Issue，给出：
1. 问题摘要
2. 可能的原因
3. 建议的解决方案

请用中文回答，格式清晰。"""

# Call AI API
print(f"Calling AI API: {os.environ['AI_BASE_URL']}")
print(f"Model: {os.environ['AI_MODEL']}")

client = OpenAI(
    api_key=os.environ["AI_API_KEY"],
    base_url=os.environ["AI_BASE_URL"]
)

response = client.chat.completions.create(
    model=os.environ["AI_MODEL"],
    messages=[
        {"role": "system", "content": "你是一个专业的 GitHub Issue 分析助手，擅长分析技术问题并给出解决方案。"},
        {"role": "user", "content": prompt}
    ],
    temperature=0.7,
    max_tokens=4096
)

answer = response.choices[0].message.content
print(f"AI response length: {len(answer)}")

# Update comment with result
body = f"""🤖 **AI 分析结果**

---

{answer}

---

🔗 [GitHub Action 运行记录](https://github.com/{repo}/actions/runs/{run_id})
<!-- Skip all labels -->"""

update_url = f"https://api.github.com/repos/{repo}/issues/comments/{comment_id}"
update_resp = requests.patch(
    update_url,
    headers=headers,
    json={"body": body}
)

if update_resp.status_code == 200:
    print("Comment updated successfully!")
else:
    print(f"Failed to update comment: {update_resp.status_code}")
    print(update_resp.text)

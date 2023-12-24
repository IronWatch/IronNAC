
```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "VisualEditor0",
            "Effect": "Allow",
            "Action": "route53:ChangeResourceRecordSets",
            "Resource": "arn:aws:route53:::hostedzone/YOUR_HOSTED_ZONE_ID_HERE",
            "Condition": {
                "ForAllValues:StringEquals": {
                    "route53:ChangeResourceRecordSetsNormalizedRecordNames": "_acme-challenge.YOUR_DOMAIN_HERE.com"
                }
            }
        },
        {
            "Sid": "VisualEditor1",
            "Effect": "Allow",
            "Action": "route53:GetChange",
            "Resource": "arn:aws:route53:::change/*"
        },
        {
            "Sid": "VisualEditor2",
            "Effect": "Allow",
            "Action": "route53:TestDNSAnswer",
            "Resource": "*"
        }
    ]
}
```